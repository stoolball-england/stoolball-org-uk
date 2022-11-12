DROP INDEX IF EXISTS [IX_StoolballPlayerInMatchStatistics_OppositionTeamRoute] ON [dbo].[StoolballPlayerInMatchStatistics]
GO
CREATE NONCLUSTERED INDEX [IX_StoolballPlayerInMatchStatistics_OppositionTeamRoute] ON [dbo].[StoolballPlayerInMatchStatistics]
(
	[OppositionTeamRoute] ASC
) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]