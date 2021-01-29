using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    // Create a migration plan for the stoolball schema
    // Each latest migration state/step is tracked for this project/feature
    public class StoolballDataMigrationPlan : MigrationPlan
    {
        public StoolballDataMigrationPlan() : base("StoolballData")
        {
            // This is the steps we need to take
            // Each step in the migration adds a unique value
            From(string.Empty)
                .To<AwardAddTable>(typeof(AwardAddTable).ToString())
                .To<ClubAddTable>(typeof(ClubAddTable).ToString())
                .To<ClubVersionAddTable>(typeof(ClubVersionAddTable).ToString())
                .To<SchoolAddTable>(typeof(SchoolAddTable).ToString())
                .To<SchoolVersionAddTable>(typeof(SchoolVersionAddTable).ToString())
                .To<MatchLocationAddTable>(typeof(MatchLocationAddTable).ToString())
                .To<CompetitionAddTable>(typeof(CompetitionAddTable).ToString())
                .To<SeasonAddTable>(typeof(SeasonAddTable).ToString())
                .To<SeasonMatchTypeAddTable>(typeof(SeasonMatchTypeAddTable).ToString())
                .To<SeasonPointsRuleAddTable>(typeof(SeasonPointsRuleAddTable).ToString())
                .To<TeamAddTable>(typeof(TeamAddTable).ToString())
                .To<TeamVersionAddTable>(typeof(TeamVersionAddTable).ToString())
                .To<TeamMatchLocationAddTable>(typeof(TeamMatchLocationAddTable).ToString())
                .To<PlayerAddTable>(typeof(PlayerAddTable).ToString())
                .To<PlayerIdentityAddTable>(typeof(PlayerIdentityAddTable).ToString())
                .To<SeasonTeamAddTable>(typeof(SeasonTeamAddTable).ToString())
                .To<SeasonPointsAdjustmentAddTable>(typeof(SeasonPointsAdjustmentAddTable).ToString())
                .To<TournamentAddTable>(typeof(TournamentAddTable).ToString())
                .To<TournamentTeamAddTable>(typeof(TournamentTeamAddTable).ToString())
                .To<TournamentSeasonAddTable>(typeof(TournamentSeasonAddTable).ToString())
                .To<MatchAddTable>(typeof(MatchAddTable).ToString())
                .To<MatchTeamAddTable>(typeof(MatchTeamAddTable).ToString())
                .To<MatchInningsAddTable>(typeof(MatchInningsAddTable).ToString())
                .To<AwardedToAddTable>(typeof(AwardedToAddTable).ToString())
                .To<PlayerInningsAddTable>(typeof(PlayerInningsAddTable).ToString())
                .To<FallOfWicketAddTable>(typeof(FallOfWicketAddTable).ToString())
                .To<OverAddTable>(typeof(OverAddTable).ToString())
                .To<BowlingFiguresAddTable>(typeof(BowlingFiguresAddTable).ToString())
                .To<CommentAddTable>(typeof(CommentAddTable).ToString())
                .To<StatisticsPlayerMatchAddTable>(typeof(StatisticsPlayerMatchAddTable).ToString())
                .To<AuditAddTable>(typeof(AuditAddTable).ToString())
                .To<NotificationSubscriptionAddTable>(typeof(NotificationSubscriptionAddTable).ToString());
        }
    }
}
