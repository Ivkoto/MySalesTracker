USE [ikostov8_totem_sales];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ScriptName nvarchar(200) = N'2026-05-17_BgnToEurConversion';
DECLARE @BgnPerEur decimal(18,5) = 1.95583;

IF COL_LENGTH('ikostov8_ssms.Sale', 'Currency') IS NULL
    OR COL_LENGTH('ikostov8_ssms.PriceRules', 'Currency') IS NULL
    OR COL_LENGTH('ikostov8_ssms.Payments', 'Currency') IS NULL
    OR COL_LENGTH('ikostov8_ssms.Expenses', 'Currency') IS NULL
    OR COL_LENGTH('ikostov8_ssms.EventDays', 'StartingPettyCashCurrency') IS NULL
BEGIN
    THROW 50001, 'Apply the required currency migrations before running this script.', 1;
END;

IF OBJECT_ID(N'ikostov8_ssms.__ManualDataFixHistory', N'U') IS NULL
BEGIN
    CREATE TABLE ikostov8_ssms.__ManualDataFixHistory
    (
        ScriptName nvarchar(200) NOT NULL PRIMARY KEY,
        ExecutedUtc datetime2 NOT NULL
    );
END;

IF EXISTS (
    SELECT 1
    FROM ikostov8_ssms.__ManualDataFixHistory
    WHERE ScriptName = @ScriptName)
BEGIN
    THROW 50002, 'This BGN to EUR conversion script has already been executed.', 1;
END;

DECLARE @SaleRows int = 0;
DECLARE @PriceRuleRows int = 0;
DECLARE @PaymentRows int = 0;
DECLARE @ExpenseRows int = 0;
DECLARE @EventDayRows int = 0;

BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE ikostov8_ssms.Sale
    SET Price = CAST(ROUND(Price / @BgnPerEur, 2) AS decimal(6, 2)),
        DiscountValue = CAST(ROUND(DiscountValue / @BgnPerEur, 2) AS decimal(6, 2)),
        Currency = 0;

    SET @SaleRows = @@ROWCOUNT;

    UPDATE ikostov8_ssms.PriceRules
    SET Price = CAST(ROUND(Price / @BgnPerEur, 2) AS decimal(6, 2)),
        Currency = 0;

    SET @PriceRuleRows = @@ROWCOUNT;

    UPDATE ikostov8_ssms.Payments
    SET Amount = CAST(ROUND(Amount / @BgnPerEur, 2) AS decimal(6, 2)),
        Currency = 0;

    SET @PaymentRows = @@ROWCOUNT;

    UPDATE ikostov8_ssms.Expenses
    SET Amount = CAST(ROUND(Amount / @BgnPerEur, 2) AS decimal(6, 2)),
        Currency = 0;

    SET @ExpenseRows = @@ROWCOUNT;

    UPDATE ikostov8_ssms.EventDays
    SET StartingPettyCash = CAST(ROUND(StartingPettyCash / @BgnPerEur, 2) AS decimal(8, 2)),
        StartingPettyCashCurrency = 0
    WHERE StartingPettyCash IS NOT NULL;

    SET @EventDayRows = @@ROWCOUNT;

    INSERT INTO ikostov8_ssms.__ManualDataFixHistory (ScriptName, ExecutedUtc)
    VALUES (@ScriptName, SYSUTCDATETIME());

    COMMIT TRANSACTION;

    SELECT
        @SaleRows AS SaleRowsUpdated,
        @PriceRuleRows AS PriceRuleRowsUpdated,
        @PaymentRows AS PaymentRowsUpdated,
        @ExpenseRows AS ExpenseRowsUpdated,
        @EventDayRows AS EventDayRowsUpdated;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;