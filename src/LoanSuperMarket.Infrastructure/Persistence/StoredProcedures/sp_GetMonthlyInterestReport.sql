-- Stored Procedure: Monthly Interest Report
-- Returns aggregated interest income per lender per month
-- Usage: EXEC sp_GetMonthlyInterestReport @LenderId, @FromDate, @ToDate

CREATE OR ALTER PROCEDURE [dbo].[sp_GetMonthlyInterestReport]
    @LenderId UNIQUEIDENTIFIER,
    @FromDate DATE,
    @ToDate DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        rs.LenderId,
        YEAR(i.PaidDate) AS [Year],
        MONTH(i.PaidDate) AS [Month],
        COUNT(i.Id) AS PaidInstallments,
        SUM(i.InterestPortion) AS TotalInterestIncome,
        SUM(i.PrincipalPortion) AS TotalPrincipalReturned,
        SUM(i.LateFeeAmount) AS TotalLateFees,
        SUM(i.InterestPortion + i.LateFeeAmount) AS TotalIncome
    FROM Installments i
    INNER JOIN RepaymentSchedules rs ON i.RepaymentScheduleId = rs.Id
    WHERE rs.LenderId = @LenderId
        AND i.[Status] = 2 -- Paid
        AND i.PaidDate IS NOT NULL
        AND i.PaidDate >= @FromDate
        AND i.PaidDate <= @ToDate
    GROUP BY rs.LenderId, YEAR(i.PaidDate), MONTH(i.PaidDate)
    ORDER BY [Year] DESC, [Month] DESC;
END
GO
