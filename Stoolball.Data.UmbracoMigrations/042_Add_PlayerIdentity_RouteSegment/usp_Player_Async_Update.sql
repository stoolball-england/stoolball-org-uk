SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
DROP PROCEDURE [dbo].[usp_Link_Player_To_Member_Async_Update]
GO

-- =============================================
-- Author:		Rick Mason
-- Create date: 17 Sept 2022
-- Description:	Checks for outstanding work following linking/unlinking a player to a member or renaming a player identity, and completes a batch of that work.
--              This is called asynchronously rather than at the time of linking/unlinking/renaming the player to avoid a slow update of 
--              StoolballPlayerInMatchStatistics causing SQL timeouts in production.
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_Player_Async_Update]
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	BEGIN TRAN

	DECLARE @DoStatisticsUpdate bit

	-- Check for a batch of up to 10 records that need to be updated with a changed details for a PlayerIdentity.
	-- The limit of 10 is to mitigate the risk of SQL timeouts updating the StoolballPlayerInMatchStatistics table in production,
	-- because the table is heavily indexed and updates can be slow.
	-- SELECT into a temp table with a separate UPDATE, rather than an all in one UPDATE...FROM statement, is the only way to limit to 10.
	SELECT TOP 10 PlayerInMatchStatisticsId, p.PlayerId, p.PlayerRoute, pi.PlayerIdentityName, s.PlayerRoute AS PlayerRouteToReplace
	INTO #LinkPlayerToMemberAsyncUpdate
	FROM StoolballPlayerInMatchStatistics s 
	INNER JOIN StoolballPlayerIdentity pi ON s.PlayerIdentityId = pi.PlayerIdentityId
	INNER JOIN StoolballPlayer p ON pi.PlayerId = p.PlayerId
	WHERE s.PlayerRoute != p.PlayerRoute OR s.PlayerId != p.PlayerId OR s.PlayerIdentityName != pi.PlayerIdentityName

	IF @@ROWCOUNT > 0
		SET @DoStatisticsUpdate = 1
	ELSE
		SET @DoStatisticsUpdate = 0
	
	IF @DoStatisticsUpdate = 1
	BEGIN
		UPDATE StoolballPlayerInMatchStatistics 
		SET 
		PlayerRoute = todo.PlayerRoute,
		PlayerId = todo.PlayerId
		FROM StoolballPlayerInMatchStatistics s
		INNER JOIN #LinkPlayerToMemberAsyncUpdate todo ON s.PlayerInMatchStatisticsId = todo.PlayerInMatchStatisticsId

		-- Return affected PlayerRoutes so that the calling code can clear the player cache, and so it knows that work was done
		-- and it might need to call this procedure again to process further records.
		SELECT PlayerRoute FROM #LinkPlayerToMemberAsyncUpdate
		UNION
		SELECT PlayerRouteToReplace AS PlayerRoute FROM #LinkPlayerToMemberAsyncUpdate
	END

	DROP TABLE #LinkPlayerToMemberAsyncUpdate

	-- When combining one player identity with another, there's a leftover player to delete that the identity used to belong to.
	-- Once the SELECT above returns no rows we know it is safe to delete these players without a blocking foreign key in the 
	-- StoolballPlayerInMatchStatistics table.
	IF @DoStatisticsUpdate = 0
	BEGIN
		SELECT p.PlayerId, p.PlayerRoute
		INTO #LinkPlayerToMemberAsyncDelete
		FROM StoolballPlayer p 
		LEFT JOIN StoolballPlayerInMatchStatistics s ON p.PlayerId = s.PlayerId
		WHERE ForAsyncDelete = 1 AND s.PlayerInMatchStatisticsId IS NULL

		IF @@ROWCOUNT > 0
		BEGIN
			DELETE FROM StoolballPlayer WHERE PlayerId IN (SELECT PlayerId FROM #LinkPlayerToMemberAsyncDelete)
	
			-- Return affected PlayerRoutes so that the calling code can clear the player cache, and so it knows that work was done
			-- and it might need to call this procedure again to process further records.
			SELECT PlayerRoute FROM #LinkPlayerToMemberAsyncDelete
		END
	
		DROP TABLE #LinkPlayerToMemberAsyncDelete
	END

	COMMIT TRAN
END
