using Umbraco.Core.Migrations;

namespace Stoolball.Data.UmbracoMigrations
{
    // Create a migration plan for the stoolball schema
    // Each latest migration state/step is tracked for this project/feature
    public class StoolballMigrationPlan : MigrationPlan
    {
        public StoolballMigrationPlan() : base("StoolballData")
        {
            // This is the steps we need to take
            // Each step in the migration adds a unique value
            From(string.Empty)
                .To<AwardAddTable>(typeof(AwardAddTable).ToString())
                .To<ClubAddTable>(typeof(ClubAddTable).ToString())
                .To<ClubNameAddTable>(typeof(ClubNameAddTable).ToString())
                .To<SchoolAddTable>(typeof(SchoolAddTable).ToString())
                .To<SchoolNameAddTable>(typeof(SchoolNameAddTable).ToString())
                .To<MatchLocationAddTable>(typeof(MatchLocationAddTable).ToString())
                .To<CompetitionAddTable>(typeof(CompetitionAddTable).ToString())
                .To<SeasonAddTable>(typeof(SeasonAddTable).ToString())
                .To<SeasonMatchTypeAddTable>(typeof(SeasonMatchTypeAddTable).ToString())
                .To<SeasonPointsRuleAddTable>(typeof(SeasonPointsRuleAddTable).ToString())
                .To<TeamAddTable>(typeof(TeamAddTable).ToString())
                .To<TeamNameAddTable>(typeof(TeamNameAddTable).ToString())
                .To<TeamMatchLocationAddTable>(typeof(TeamMatchLocationAddTable).ToString())
                .To<SeasonTeamAddTable>(typeof(SeasonTeamAddTable).ToString())
                .To<SeasonPointsAdjustmentAddTable>(typeof(SeasonPointsAdjustmentAddTable).ToString())
                .To<SeasonAwardAddTable>(typeof(SeasonAwardAddTable).ToString())
                .To<PlayerAddTable>(typeof(PlayerAddTable).ToString())
                .To<PlayerIdentityAddTable>(typeof(PlayerIdentityAddTable).ToString())
                .To<TournamentAddTable>(typeof(TournamentAddTable).ToString())
                .To<TournamentTeamAddTable>(typeof(TournamentTeamAddTable).ToString())
                .To<TournamentSeasonAddTable>(typeof(TournamentSeasonAddTable).ToString())
                .To<TournamentCommentAddTable>(typeof(TournamentCommentAddTable).ToString())
                .To<TournamentAwardAddTable>(typeof(TournamentAwardAddTable).ToString())
                .To<MatchAddTable>(typeof(MatchAddTable).ToString())
                .To<MatchTeamAddTable>(typeof(MatchTeamAddTable).ToString())
                .To<MatchInningsAddTable>(typeof(MatchInningsAddTable).ToString())
                .To<MatchAwardAddTable>(typeof(MatchAwardAddTable).ToString())
                .To<PlayerInningsAddTable>(typeof(PlayerInningsAddTable).ToString())
                .To<FallOfWicketAddTable>(typeof(FallOfWicketAddTable).ToString())
                .To<OverAddTable>(typeof(OverAddTable).ToString())
                .To<BowlingAddTable>(typeof(BowlingAddTable).ToString())
                .To<MatchCommentAddTable>(typeof(MatchCommentAddTable).ToString())
                .To<StatisticsPlayerMatchAddTable>(typeof(StatisticsPlayerMatchAddTable).ToString())
                .To<AuditAddTable>(typeof(AuditAddTable).ToString())
                .To<NotificationSubscriptionAddTable>(typeof(NotificationSubscriptionAddTable).ToString());
        }
    }
}
