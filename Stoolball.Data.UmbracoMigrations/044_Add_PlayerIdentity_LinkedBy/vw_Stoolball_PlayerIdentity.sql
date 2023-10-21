SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


ALTER VIEW [dbo].[vw_Stoolball_PlayerIdentity]
AS
SELECT pi.PlayerIdentityId, pi.TeamId, pi.PlayerIdentityName, pi.ComparableName, pi.RouteSegment, pi.LinkedBy, p.PlayerId, p.PlayerRoute, p.MemberKey
FROM StoolballPlayerIdentity pi INNER JOIN StoolballPlayer p ON pi.PlayerId = p.PlayerId
WHERE pi.Deleted = 0 AND p.Deleted = 0
GO