﻿using Stoolball.Teams;
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
    public class TeamMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly ITeamDataMigrator _teamDataMigrator;

        public TeamMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            ITeamDataMigrator teamDataMigrator) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _teamDataMigrator = teamDataMigrator;
        }

        [HttpPost]
        public void CreateTeam(Team team)
        {
            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            _teamDataMigrator.MigrateTeam(team);
        }

        [HttpDelete]
        public void DeleteTeams()
        {
            _teamDataMigrator.DeleteTeams();
        }
    }
}