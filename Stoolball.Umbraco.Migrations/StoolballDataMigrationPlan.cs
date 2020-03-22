using Umbraco.Core.Migrations;

namespace Stoolball.Umbraco.Data
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
                .To<AuditAddTable>(typeof(AuditAddTable).ToString())
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
                .To<PlayerIdentityAddTable>(typeof(PlayerIdentityAddTable).ToString())
                .To<MatchAddTable>(typeof(MatchAddTable).ToString())
                .To<MatchInningsAddTable>(typeof(MatchInningsAddTable).ToString())
                .To<MatchAwardTypeAddTable>(typeof(MatchAwardTypeAddTable).ToString())
                .To<MatchAwardAddTable>(typeof(MatchAwardAddTable).ToString())
                .To<MatchTeamAddTable>(typeof(MatchTeamAddTable).ToString())
                .To<SeasonMatchAddTable>(typeof(SeasonMatchAddTable).ToString())
                .To<BattingAddTable>(typeof(BattingAddTable).ToString())
                .To<BowlingOverAddTable>(typeof(BowlingOverAddTable).ToString())
                .To<MatchCommentAddTable>(typeof(MatchCommentAddTable).ToString());
        }
    }
}
