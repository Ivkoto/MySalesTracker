## Migration Review

## Execution Context

The current `Docs/BgnToEurConversion.sql` file is written for a specific hosted SQL Server database and schema.
Before running it in any environment, verify that the hardcoded database and schema names in the script still match the target.

## Currency Rollout Migrations

The BGN -> EUR rollout now depends on these migrations:

1. `MySalesTracker.Infrastructure/Migrations/20260516221949_AddCurrencyColumn.cs`

This migration adds `Currency` to:

- `Sale`
- `PriceRules`
- `Payments`
- `Expenses`

2. `MySalesTracker.Infrastructure/Migrations/20260516233840_AddCurrencyDefaultValues.cs`

This migration adds default `EUR` values for the persisted currency columns introduced above.

3. `MySalesTracker.Infrastructure/Migrations/20260517135548_AddStartingPettyCashCurrency.cs`

This migration adds `EventDays.StartingPettyCashCurrency`.

It is not sufficient for the historical BGN -> EUR transition because it does not convert the stored monetary values.

That means old data would become semantically wrong after the migration:

- old BGN amounts would still be stored as the old numeric values
- the new `Currency` columns would default to `EUR`
- the UI would render those old BGN numbers as EUR

There is one more persisted money field that must also be converted even though it has no `Currency` column:

- `EventDays.StartingPettyCash`

## Execution Order

1. Take a database backup.
2. Apply all required currency schema migrations.
3. Run the SQL script in `Docs/BgnToEurConversion.sql` once.
4. Validate a few known historical rows and the main summary screens.

## Scope of the Conversion Script

The conversion script updates these fields using the fixed rate `1 EUR = 1.95583 BGN`:

- `Sale.Price`
- `Sale.DiscountValue`
- `PriceRules.Price`
- `Payments.Amount`
- `Expenses.Amount`
- `EventDays.StartingPettyCash`

The script also force-sets the persisted currency columns to `EUR` (`0`) for:

- `Sale.Currency`
- `PriceRules.Currency`
- `Payments.Currency`
- `Expenses.Currency`
- `EventDays.StartingPettyCashCurrency`

## Notes

- The script is written for SQL Server, which matches the current provider configuration.
- The script is intended to run once only.
- The script creates a small guard table `ikostov8_ssms.__ManualDataFixHistory` so a second execution fails instead of silently converting the data twice.