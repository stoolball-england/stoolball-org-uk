UPDATE StoolballPlayerIdentity SET LinkedBy = 'Member' WHERE PlayerId IN (
	SELECT PlayerId FROM StoolballPlayerIdentity GROUP BY PlayerId HAVING COUNT(PlayerId) > 1
)