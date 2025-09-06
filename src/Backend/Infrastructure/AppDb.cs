
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure;

public class AppDb : DbContext
{
    public AppDb(DbContextOptions<AppDb> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeItem> RecipeItems => Set<RecipeItem>();
    public DbSet<ProductionPlan> ProductionPlans => Set<ProductionPlan>();
    public DbSet<ProductionItem> ProductionItems => Set<ProductionItem>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasPostgresExtension("uuid-ossp");

        b.Entity<Product>().ToTable("products").HasKey(x => x.ProductId);
        b.Entity<Product>().Property(x => x.ProductId).HasColumnName("product_id");
        b.Entity<Product>().Property(x => x.Name).HasColumnName("name");
        b.Entity<Product>().Property(x => x.Unit).HasColumnName("unit");

        b.Entity<Batch>().ToTable("batches").HasKey(x => x.BatchId);
        b.Entity<Batch>().Property(x => x.BatchId).HasColumnName("batch_id");
        b.Entity<Batch>().Property(x => x.ProductId).HasColumnName("product_id");
        b.Entity<Batch>().Property(x => x.ExpiryDate).HasColumnName("expiry_date");
        b.Entity<Batch>().Property(x => x.QtyAvailable).HasColumnName("qty_available");

        b.Entity<Recipe>().ToTable("recipes").HasKey(x => x.RecipeId);
        b.Entity<Recipe>().Property(x => x.RecipeId).HasColumnName("recipe_id");
        b.Entity<Recipe>().Property(x => x.Name).HasColumnName("name");

        b.Entity<RecipeItem>().ToTable("recipe_items").HasKey(x => new { x.RecipeId, x.ProductId, x.DietVariant });
        b.Entity<RecipeItem>().Property(x => x.RecipeId).HasColumnName("recipe_id");
        b.Entity<RecipeItem>().Property(x => x.ProductId).HasColumnName("product_id");
        b.Entity<RecipeItem>().Property(x => x.QtyPerPortion).HasColumnName("qty_per_portion");
        b.Entity<RecipeItem>().Property(x => x.LossPct).HasColumnName("loss_pct");
        b.Entity<RecipeItem>().Property(x => x.DietVariant).HasColumnName("diet_variant");

        b.Entity<ProductionPlan>().ToTable("production_plans").HasKey(x => x.PlanId);
        b.Entity<ProductionPlan>().Property(x => x.PlanId).HasColumnName("plan_id");
        b.Entity<ProductionPlan>().Property(x => x.PlanDate).HasColumnName("plan_date");
        b.Entity<ProductionPlan>().Property(x => x.Status).HasColumnName("status");

        b.Entity<ProductionItem>().ToTable("production_items").HasKey(x => new { x.PlanId, x.RecipeId, x.DietVariant });
        b.Entity<ProductionItem>().Property(x => x.PlanId).HasColumnName("plan_id");
        b.Entity<ProductionItem>().Property(x => x.RecipeId).HasColumnName("recipe_id");
        b.Entity<ProductionItem>().Property(x => x.DietVariant).HasColumnName("diet_variant");
        b.Entity<ProductionItem>().Property(x => x.Portions).HasColumnName("portions");

        b.Entity<InventoryReservation>().ToTable("inventory_reservations").HasKey(x => x.ReservationId);
        b.Entity<InventoryReservation>().Property(x => x.ReservationId).HasColumnName("reservation_id");
        b.Entity<InventoryReservation>().Property(x => x.ProductId).HasColumnName("product_id");
        b.Entity<InventoryReservation>().Property(x => x.PlanId).HasColumnName("plan_id");
        b.Entity<InventoryReservation>().Property(x => x.QtyReserved).HasColumnName("qty_reserved");

        b.Entity<InventoryTransaction>().ToTable("inventory_transactions").HasKey(x => x.TxnId);
        b.Entity<InventoryTransaction>().Property(x => x.TxnId).HasColumnName("txn_id");
        b.Entity<InventoryTransaction>().Property(x => x.ProductId).HasColumnName("product_id");
        b.Entity<InventoryTransaction>().Property(x => x.BatchId).HasColumnName("batch_id");
        b.Entity<InventoryTransaction>().Property(x => x.Type).HasColumnName("type");
        b.Entity<InventoryTransaction>().Property(x => x.Qty).HasColumnName("qty");
        b.Entity<InventoryTransaction>().Property(x => x.CreatedAt).HasColumnName("created_at");
    }
}

public class Product
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "kg";
}

public class Batch
{
    public Guid BatchId { get; set; }
    public Guid ProductId { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal QtyAvailable { get; set; }
}

public class Recipe
{
    public Guid RecipeId { get; set; }
    public string Name { get; set; } = "";
}

public class RecipeItem
{
    public Guid RecipeId { get; set; }
    public Guid ProductId { get; set; }
    public decimal QtyPerPortion { get; set; }
    public decimal LossPct { get; set; }
    public string DietVariant { get; set; } = "standard";
}

public class ProductionPlan
{
    public Guid PlanId { get; set; }
    public DateTime PlanDate { get; set; }
    public string Status { get; set; } = "Planned";
}

public class ProductionItem
{
    public Guid PlanId { get; set; }
    public Guid RecipeId { get; set; }
    public string DietVariant { get; set; } = "standard";
    public int Portions { get; set; }
}

public class InventoryReservation
{
    public Guid ReservationId { get; set; }
    public Guid ProductId { get; set; }
    public Guid PlanId { get; set; }
    public decimal QtyReserved { get; set; }
}

public class InventoryTransaction
{
    public Guid TxnId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? BatchId { get; set; }
    public string Type { get; set; } = "RW"; // PZ/RW/WZ/INV/ADJ
    public decimal Qty { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
