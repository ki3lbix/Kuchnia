
# Backend – komplet z EF Core i FEFO (GitHub-first)

Ten pakiet uzupełnia wcześniejszy starter o:
- EF Core (Npgsql) i `AppDb`
- `InventoryService` z rezerwacjami i konsumpcją FEFO
- Endpoints: `/inventory/reserve`, `/inventory/consume`, `/production/plan*`
- `02-reservations.sql` (miękkie rezerwacje)
- CI (`.github/workflows/ci.yml`) i Codespaces (`.devcontainer/devcontainer.json`)

## Uruchomienie dev (lokalnie lub Codespaces)
```bash
docker compose -f docker-compose.dev.yml up -d
chmod +x ops/dev-seed.sh && ./ops/dev-seed.sh
export ConnectionStrings__Default='Host=localhost;Port=5432;Database=catering;Username=app;Password=app'
dotnet run --project ./src/Backend/Backend.csproj
# Swagger: http://localhost:5000/swagger
```

## Scenariusz demo
1) POST `/production/plan` → utwórz plan na dziś.
2) POST `/production/plan/{planId}/item` z `recipeId` „Spaghetti”, `portions`: 10.
3) POST `/inventory/reserve` `{ "planId": "..." }` → dostaniesz rezerwacje i ewentualne braki.
4) POST `/inventory/consume` `{ "planId": "..." }` → towar zostanie zdjęty z partii (FEFO) i zapisany w `inventory_transactions`.

## Migrations (opcjonalnie)
Na testach korzystamy z `schema.sql` + `02-reservations.sql`. 
Jeśli chcesz przejść na code‑first:
```bash
dotnet tool install --global dotnet-ef
dotnet add ./src/Backend/Backend.csproj package Microsoft.EntityFrameworkCore.Design
dotnet ef migrations add Init --project ./src/Backend/Backend.csproj
dotnet ef database update
```

## Bezpieczeństwo i zgodność
- Zmienna `ConnectionStrings__Default` w ENV/secrets.
- Brak PII dzieci – operujemy na agregatach (porcje).
- Audyt: transakcyjne rozchody i logi w CI.
