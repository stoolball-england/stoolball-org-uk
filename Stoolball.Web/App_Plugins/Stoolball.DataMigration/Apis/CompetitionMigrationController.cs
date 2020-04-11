using Stoolball.Competitions;
using Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators;
using System;
using System.Web.Http;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Editors;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.Apis
{
    [PluginController("Migration")]
    public class CompetitionMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly ICompetitionDataMigrator _competitionDataMigrator;

        public CompetitionMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            ICompetitionDataMigrator competitionDataMigrator) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _competitionDataMigrator = competitionDataMigrator;
        }

        [HttpPost]
        public void CreateCompetition(Competition competition)
        {
            if (competition is null)
            {
                throw new ArgumentNullException(nameof(competition));
            }

            _competitionDataMigrator.MigrateCompetition(competition);
        }

        [HttpDelete]
        public void DeleteCompetitions()
        {
            _competitionDataMigrator.DeleteCompetitions();
        }
    }
}
