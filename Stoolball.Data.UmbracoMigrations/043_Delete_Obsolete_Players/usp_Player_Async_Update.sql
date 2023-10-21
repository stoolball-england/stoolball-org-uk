SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		Rick Mason
-- Create date: 17 Sept 2022
-- Updated:		16 Jan 2023
-- Description:	Checks for outstanding work following:
--
--					* linking/unlinking a player to a member 
--					* renaming a player identity
--					* removing the last reference to a player identity from scorecards
--
--				and completes a batch of that work.
--
--              This is called asynchronously rather than at the time of the original action to avoid 
--              a slow update of StoolballPlayerInMatchStatistics causing SQL timeouts in production.
-- =============================================
ALTER   PROCEDURE [dbo].[usp_Player_Async_Update]
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
	INTO #PlayerAsyncUpdate1
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
		PlayerRoute = #PlayerAsyncUpdate1.PlayerRoute,
		PlayerIdentityName = #PlayerAsyncUpdate1.PlayerIdentityName,
		PlayerId = #PlayerAsyncUpdate1.PlayerId
		FROM StoolballPlayerInMatchStatistics s
		INNER JOIN #PlayerAsyncUpdate1 ON s.PlayerInMatchStatisticsId = #PlayerAsyncUpdate1.PlayerInMatchStatisticsId

		-- Return affected PlayerRoutes so that the calling code can clear the player cache, and so it knows that work was done
		-- and it might need to call this procedure again to process further records.
		SELECT PlayerRoute FROM #PlayerAsyncUpdate1
		UNION
		SELECT PlayerRouteToReplace AS PlayerRoute FROM #PlayerAsyncUpdate1
	END

	DROP TABLE #PlayerAsyncUpdate1

	-- Same again, but for PlayerIdentities that appear in the fielding columns of the StoolballPlayerInMatchStatistics table.
	IF @DoStatisticsUpdate = 0
	BEGIN
		SELECT TOP 10 PlayerInMatchStatisticsId, p.PlayerId, p.PlayerRoute, pi.PlayerIdentityName, s.BowledByPlayerRoute AS PlayerRouteToReplace
		INTO #PlayerAsyncUpdate2
		FROM StoolballPlayerInMatchStatistics s 
		INNER JOIN StoolballPlayerIdentity pi ON s.BowledByPlayerIdentityId = pi.PlayerIdentityId
		INNER JOIN StoolballPlayer p ON pi.PlayerId = p.PlayerId
		WHERE s.BowledByPlayerIdentityId IS NOT NULL AND s.BowledByPlayerRoute != p.PlayerRoute OR s.BowledByPlayerIdentityName != pi.PlayerIdentityName

		IF @@ROWCOUNT > 0
			SET @DoStatisticsUpdate = 1
		ELSE
			SET @DoStatisticsUpdate = 0
	
		IF @DoStatisticsUpdate = 1
		BEGIN
			UPDATE StoolballPlayerInMatchStatistics 
			SET 
			BowledByPlayerRoute = #PlayerAsyncUpdate2.PlayerRoute,
			BowledByPlayerIdentityName = #PlayerAsyncUpdate2.PlayerIdentityName
			FROM StoolballPlayerInMatchStatistics s
			INNER JOIN #PlayerAsyncUpdate2 ON s.PlayerInMatchStatisticsId = #PlayerAsyncUpdate2.PlayerInMatchStatisticsId

			-- Return affected PlayerRoutes so that the calling code can clear the player cache, and so it knows that work was done
			-- and it might need to call this procedure again to process further records.
			SELECT PlayerRoute FROM #PlayerAsyncUpdate2
			UNION
			SELECT PlayerRouteToReplace AS PlayerRoute FROM #PlayerAsyncUpdate2
		END

		DROP TABLE #PlayerAsyncUpdate2
	END

	IF @DoStatisticsUpdate = 0
	BEGIN
		SELECT TOP 10 PlayerInMatchStatisticsId, p.PlayerId, p.PlayerRoute, pi.PlayerIdentityName, s.CaughtByPlayerRoute AS PlayerRouteToReplace
		INTO #PlayerAsyncUpdate3
		FROM StoolballPlayerInMatchStatistics s 
		INNER JOIN StoolballPlayerIdentity pi ON s.CaughtByPlayerIdentityId = pi.PlayerIdentityId
		INNER JOIN StoolballPlayer p ON pi.PlayerId = p.PlayerId
		WHERE s.CaughtByPlayerIdentityId IS NOT NULL AND s.CaughtByPlayerRoute != p.PlayerRoute OR s.CaughtByPlayerIdentityName != pi.PlayerIdentityName

		IF @@ROWCOUNT > 0
			SET @DoStatisticsUpdate = 1
		ELSE
			SET @DoStatisticsUpdate = 0
	
		IF @DoStatisticsUpdate = 1
		BEGIN
			UPDATE StoolballPlayerInMatchStatistics 
			SET 
			CaughtByPlayerRoute = #PlayerAsyncUpdate3.PlayerRoute,
			CaughtByPlayerIdentityName = #PlayerAsyncUpdate3.PlayerIdentityName
			FROM StoolballPlayerInMatchStatistics s
			INNER JOIN #PlayerAsyncUpdate3 ON s.PlayerInMatchStatisticsId = #PlayerAsyncUpdate3.PlayerInMatchStatisticsId

			-- Return affected PlayerRoutes so that the calling code can clear the player cache, and so it knows that work was done
			-- and it might need to call this procedure again to process further records.
			SELECT PlayerRoute FROM #PlayerAsyncUpdate3
			UNION
			SELECT PlayerRouteToReplace AS PlayerRoute FROM #PlayerAsyncUpdate3
		END

		DROP TABLE #PlayerAsyncUpdate3
	END

	IF @DoStatisticsUpdate = 0
	BEGIN
		SELECT TOP 10 PlayerInMatchStatisticsId, p.PlayerId, p.PlayerRoute, pi.PlayerIdentityName, s.RunOutByPlayerRoute AS PlayerRouteToReplace
		INTO #PlayerAsyncUpdate4
		FROM StoolballPlayerInMatchStatistics s 
		INNER JOIN StoolballPlayerIdentity pi ON s.RunOutByPlayerIdentityId = pi.PlayerIdentityId
		INNER JOIN StoolballPlayer p ON pi.PlayerId = p.PlayerId
		WHERE s.RunOutByPlayerIdentityId IS NOT NULL AND s.RunOutByPlayerRoute != p.PlayerRoute OR s.RunOutByPlayerIdentityName != pi.PlayerIdentityName

		IF @@ROWCOUNT > 0
			SET @DoStatisticsUpdate = 1
		ELSE
			SET @DoStatisticsUpdate = 0
	
		IF @DoStatisticsUpdate = 1
		BEGIN
			UPDATE StoolballPlayerInMatchStatistics 
			SET 
			RunOutByPlayerRoute = #PlayerAsyncUpdate4.PlayerRoute,
			RunOutByPlayerIdentityName = #PlayerAsyncUpdate4.PlayerIdentityName
			FROM StoolballPlayerInMatchStatistics s
			INNER JOIN #PlayerAsyncUpdate4 ON s.PlayerInMatchStatisticsId = #PlayerAsyncUpdate4.PlayerInMatchStatisticsId

			-- Return affected PlayerRoutes so that the calling code can clear the player cache, and so it knows that work was done
			-- and it might need to call this procedure again to process further records.
			SELECT PlayerRoute FROM #PlayerAsyncUpdate4
			UNION
			SELECT PlayerRouteToReplace AS PlayerRoute FROM #PlayerAsyncUpdate4
		END

		DROP TABLE #PlayerAsyncUpdate4
	END

	-- When combining one player identity with another, there's a leftover player to delete that the identity used to belong to.
	-- Once the SELECT above returns no rows we know it is safe to delete these players without a blocking foreign key in the 
	-- StoolballPlayerInMatchStatistics table.
	IF @DoStatisticsUpdate = 0
	BEGIN
		SELECT p.PlayerId, p.PlayerRoute
		INTO #PlayerAsyncDelete
		FROM StoolballPlayer p 
		LEFT JOIN StoolballPlayerInMatchStatistics s ON p.PlayerId = s.PlayerId
		WHERE ForAsyncDelete = 1 AND s.PlayerInMatchStatisticsId IS NULL

		IF @@ROWCOUNT > 0
			SET @DoStatisticsUpdate = 1
		ELSE
			SET @DoStatisticsUpdate = 0
	
		IF @DoStatisticsUpdate = 1
		BEGIN
			DELETE FROM StoolballPlayer WHERE PlayerId IN (SELECT PlayerId FROM #PlayerAsyncDelete)
	
			-- Return affected PlayerRoutes so that the calling code can clear the player cache, and so it knows that work was done
			-- and it might need to call this procedure again to process further records.
			SELECT PlayerRoute FROM #PlayerAsyncDelete
		END
	
		DROP TABLE #PlayerAsyncDelete
	END

	-- When updating a scorecard, the last reference to a PlayerIdentity might be removed and it gets marked as Deleted = 1.
	-- Confirm there are no remaining statistics then delete the identity and, if it has no further identities, the player too.
	IF @DoStatisticsUpdate = 0
	BEGIN
		SELECT p.PlayerId, p.PlayerRoute, pi.PlayerIdentityId
		INTO #PlayerIdentityAsyncDelete
		FROM StoolballPlayer p 
		INNER JOIN StoolballPlayerIdentity pi ON p.PlayerId = pi.PlayerId
		LEFT JOIN StoolballPlayerInMatchStatistics s ON 
			(pi.PlayerIdentityId = s.PlayerIdentityId OR
			 pi.PlayerIdentityId = s.BowledByPlayerIdentityId OR 
			 pi.PlayerIdentityId = s.CaughtByPlayerIdentityId OR 
			 pi.PlayerIdentityId = s.RunOutByPlayerIdentityId)
		WHERE pi.Deleted = 1 AND s.PlayerInMatchStatisticsId IS NULL

		IF @@ROWCOUNT > 0
		BEGIN
			BEGIN TRAN
			
			DELETE FROM StoolballPlayerIdentity WHERE PlayerIdentityId IN (SELECT PlayerIdentityId FROM #PlayerIdentityAsyncDelete)
			DELETE FROM StoolballPlayer WHERE PlayerId IN (
				SELECT p.PlayerId FROM StoolballPlayer p 
				LEFT JOIN StoolballPlayerIdentity pi ON p.PlayerId = pi.PlayerId 
				GROUP BY p.PlayerId
				HAVING COUNT(PlayerIdentityId) = 0
			) 
			AND PlayerId IN (SELECT PlayerId FROM #PlayerIdentityAsyncDelete)
			
			COMMIT TRAN

			-- Return affected PlayerRoutes so that the calling code can clear the player cache, and so it knows that work was done
			-- and it might need to call this procedure again to process further records.
			SELECT PlayerRoute FROM #PlayerIdentityAsyncDelete
		END
	
		DROP TABLE #PlayerIdentityAsyncDelete
	END


	COMMIT TRAN
END
