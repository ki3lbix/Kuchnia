
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

using Backend.Infrastructure;

public class InventoryService
{
    private readonly AppDb _db;
    private readonly ILogger<InventoryService> _log;

    public InventoryService(AppDb db, ILogger<InventoryService> log)
    {
        _db = db;
        _log = log;
    }

    public sealed record ReserveResult(Guid PlanId, List<ReservedLine> Reserved, List<ShortageLine> Shortages);
    public sealed record ReservedLine(Guid ProductId, decimal Qty);
    public sealed record ShortageLine(Guid ProductId, decimal MissingQty);

    public async Task<ReserveResult> ReserveAsync(Guid planId, CancellationToken ct = default)
    {
        // Policzenie zapotrzebowania: sumujemy po ProductionItems x RecipeItems
        var requirements = await (
            from pi in _db.ProductionItems
            join ri in _db.RecipeItems on new { pi.RecipeId, pi.DietVariant } equals new { ri.RecipeId, ri.DietVariant }
            select new {
                pi.PlanId,
                ri.ProductId,
                Qty = (decimal)pi.Portions * ri.QtyPerPortion * (1 + ri.LossPct / 100m)
            }
        )
        .Where(x => x.PlanId == planId)
        .GroupBy(x => new { x.PlanId, x.ProductId })
        .Select(g => new { g.Key.PlanId, g.Key.ProductId, Qty = g.Sum(x => x.Qty) })
        .ToListAsync(ct);

        var reserved = new List<ReservedLine>();
        var shortages = new List<ShortageLine>();

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // wyczyść poprzednie rezerwacje dla tego planu (idempotencja)
            var old = _db.InventoryReservations.Where(r => r.PlanId == planId);
            _db.InventoryReservations.RemoveRange(old);
            await _db.SaveChangesAsync(ct);

            foreach (var req in requirements)
            {
                // dostępny stan (suma po partiach)
                var available = await _db.Batches
                    .Where(b => b.ProductId == req.ProductId && b.QtyAvailable > 0)
                    .SumAsync(b => (decimal?)b.QtyAvailable, ct) ?? 0m;

                var reserveQty = Math.Min(available, req.Qty);
                if (reserveQty > 0)
                {
                    _db.InventoryReservations.Add(new Infrastructure.InventoryReservation
                    {
                        PlanId = planId,
                        ProductId = req.ProductId,
                        QtyReserved = reserveQty
                    });
                    reserved.Add(new ReservedLine(req.ProductId, reserveQty));
                }
                if (available < req.Qty)
                {
                    shortages.Add(new ShortageLine(req.ProductId, req.Qty - available));
                }
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "ReserveAsync failed for plan {PlanId}", planId);
            await tx.RollbackAsync(ct);
            throw;
        }

        return new ReserveResult(planId, reserved, shortages);
    }

    public sealed record ConsumeResult(Guid PlanId, List<ConsumptionTxn> Transactions);
    public sealed record ConsumptionTxn(Guid TxnId, Guid ProductId, Guid BatchId, decimal Qty);

    public async Task<ConsumeResult> ConsumeAsync(Guid planId, CancellationToken ct = default)
    {
        // Pobierz wymagania i rezerwacje (jeśli brak rezerwacji, użyj wymagania)
        var requirements = await (
            from pi in _db.ProductionItems
            join ri in _db.RecipeItems on new { pi.RecipeId, pi.DietVariant } equals new { ri.RecipeId, ri.DietVariant }
            where pi.PlanId == planId
            group new { pi, ri } by new { pi.PlanId, ri.ProductId } into g
            select new { g.Key.PlanId, g.Key.ProductId, Qty = g.Sum(x => (decimal)x.pi.Portions * x.ri.QtyPerPortion * (1 + x.ri.LossPct/100m)) }
        ).ToListAsync(ct);

        var reservations = await _db.InventoryReservations
            .Where(r => r.PlanId == planId)
            .ToListAsync(ct);

        var txns = new List<ConsumptionTxn>();

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            foreach (var req in requirements)
            {
                var target = reservations.FirstOrDefault(r => r.ProductId == req.ProductId)?.QtyReserved ?? req.Qty;
                if (target <= 0) continue;

                // FEFO: partie po najbliższej dacie ważności
                var batches = await _db.Batches
                    .Where(b => b.ProductId == req.ProductId && b.QtyAvailable > 0)
                    .OrderBy(b => b.ExpiryDate) // NULLS LAST nie w EF, ale null będzie na końcu domyślnie dla DateTime?
                    .ToListAsync(ct);

                var remaining = target;

                foreach (var b in batches)
                {
                    if (remaining <= 0) break;
                    var take = Math.Min(b.QtyAvailable, remaining);
                    if (take <= 0) continue;

                    b.QtyAvailable -= take;
                    var txn = new Infrastructure.InventoryTransaction
                    {
                        TxnId = Guid.NewGuid(),
                        ProductId = req.ProductId,
                        BatchId = b.BatchId,
                        Qty = take,
                        Type = "RW",
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.InventoryTransactions.Add(txn);
                    txns.Add(new ConsumptionTxn(txn.TxnId, req.ProductId, b.BatchId, take));
                    remaining -= take;
                }

                if (remaining > 0)
                {
                    throw new InvalidOperationException($"Brak wystarczającej ilości dla produktu {req.ProductId}. Brakuje {remaining}.");
                }
            }

            // po konsumpcji usuń rezerwacje planu (zostały zużyte)
            var old = _db.InventoryReservations.Where(r => r.PlanId == planId);
            _db.InventoryReservations.RemoveRange(old);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "ConsumeAsync failed for plan {PlanId}", planId);
            await tx.RollbackAsync(ct);
            throw;
        }

        return new ConsumeResult(planId, txns);
    }
}
