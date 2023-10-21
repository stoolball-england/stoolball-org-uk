BEGIN TRAN

UPDATE StoolballPlayerIdentity SET Deleted = 1 WHERE PlayerIdentityId IN (
	SELECT pi.PlayerIdentityId FROM StoolballPlayerIdentity pi
	WHERE 
		(SELECT COUNT(PlayerInningsId) FROM StoolballPlayerInnings WHERE BatterPlayerIdentityId = pi.PlayerIdentityId OR DismissedByPlayerIdentityId = pi.PlayerIdentityId OR BowlerPlayerIdentityId = pi.PlayerIdentityId) = 0 AND
		(SELECT COUNT(OverId) FROM StoolballOver WHERE BowlerPlayerIdentityId = pi.PlayerIdentityId) = 0 AND
		(SELECT COUNT(AwardedToId) FROM StoolballAwardedTo WHERE PlayerIdentityId = pi.PlayerIdentityId) = 0
)

EXEC usp_Player_Async_Update

COMMIT TRAN