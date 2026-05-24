-- Stored Procedure: Platform Summary Dashboard
-- Returns high-level platform KPIs in a single query
-- Usage: EXEC sp_GetPlatformSummary

CREATE OR ALTER PROCEDURE [dbo].[sp_GetPlatformSummary]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        (SELECT COUNT(*) FROM RepaymentSchedules WHERE Performance != 3) AS ActiveLoans,
        (SELECT COUNT(*) FROM RepaymentSchedules WHERE Performance = 3) AS DefaultedLoans,
        (SELECT ISNULL(SUM(FundedAmount), 0) FROM RepaymentSchedules) AS TotalFunded,
        (SELECT ISNULL(SUM(i.PaidAmount), 0) FROM Installments i WHERE i.[Status] = 2) AS TotalCollected,
        (SELECT ISNULL(SUM(i.InterestPortion), 0) FROM Installments i WHERE i.[Status] = 2) AS TotalInterestCollected,
        (SELECT ISNULL(SUM(i.LateFeeAmount), 0) FROM Installments i WHERE i.[Status] = 2 AND i.LateFeeAmount > 0) AS TotalLateFeesCollected,
        (SELECT COUNT(*) FROM Lenders WHERE [Status] = 3) AS ActiveLenders,
        (SELECT COUNT(*) FROM Borrowers WHERE [Status] = 3) AS ActiveBorrowers,
        (SELECT ISNULL(SUM(AvailableFunds), 0) FROM Lenders WHERE [Status] = 3) AS TotalAvailableCapital;
END
GO
