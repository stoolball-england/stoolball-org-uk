-- =============================================
-- Author:		Rick Mason
-- Create date: 27 June 2022
-- Description:	Return the best name for a team at a given date
-- =============================================
CREATE OR ALTER FUNCTION [dbo].[fn_TeamName] 
(
    @TeamId uniqueidentifier,
    @Date datetime
)
RETURNS nvarchar(255)
AS
BEGIN
    DECLARE @TeamName nvarchar(255)

    SELECT @TeamName = ISNULL(
        -- SELECT the TeamName at the given date
        (SELECT TOP 1 TeamName FROM StoolballTeamVersion WHERE TeamId = @TeamId AND ((FromDate <= @Date AND UntilDate >= @Date) OR (FromDate <= @Date AND UntilDate IS NULL)) ORDER BY UntilDate DESC), 

        -- If the above query returns NULL, it must be a date outside the range of a team's existence. This is probably bad data, so return the best available name.

        (SELECT TOP 1
            CASE WHEN UntilDate > @Date 
            THEN (SELECT TOP 1 TeamName FROM StoolballTeamVersion WHERE TeamId = @TeamId ORDER BY ISNULL(UntilDate,'3000-01-01') ASC) -- If @Date is before the team started, return the team's first TeamName. 
            ELSE (SELECT TOP 1 TeamName FROM StoolballTeamVersion WHERE TeamId = @TeamId ORDER BY ISNULL(UntilDate,'3000-01-01') DESC) -- If it's later the team must be expired, so return its final expired name.
            END 
            FROM StoolballTeamVersion WHERE TeamId = @TeamId ORDER BY UntilDate DESC)
        ) 

    RETURN @TeamName
END