UPDATE StoolballPlayerIdentity
SET StoolballPlayerIdentity.RouteSegment = Results.RouteSegment
FROM StoolballPlayerIdentity
INNER JOIN (
	SELECT PlayerIdentityId, CASE WHEN RouteSegment = '' THEN CAST(PlayerIdentityId AS varchar(255)) ELSE RouteSegment END AS RouteSegment FROM 
	(
		SELECT PlayerIdentityId,
		CASE
		WHEN RIGHT(RouteSegment,1) = '-' THEN LEFT(RouteSegment, LEN(RouteSegment)-1)
		ELSE RouteSegment
		END
		AS RouteSegment FROM (
			SELECT PlayerIdentityId, LOWER(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(PlayerIdentityName,' ','-'),'''',''),'`',''),'&',''),'.',''),'(',''),')',''),'/','-'),'#',''),';',''),':',''),'?',''),'|','')))) AS RouteSegment 
			FROM StoolballPlayerIdentity
		) AS Results
	) AS Results
) AS Results
ON StoolballPlayerIdentity.PlayerIdentityId = Results.PlayerIdentityId;

ALTER TABLE StoolballPlayerIdentity
ALTER COLUMN RouteSegment nvarchar(255) NOT NULL