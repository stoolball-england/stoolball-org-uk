﻿using Stoolball.Clubs;
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
    public class ClubMigrationController : UmbracoAuthorizedJsonController
    {
        private readonly IClubDataMigrator _clubDataMigrator;

        public ClubMigrationController(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISqlContext sqlContext,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            IRuntimeState runtimeState,
            UmbracoHelper umbracoHelper,
            IClubDataMigrator clubDataMigrator) :
            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper)
        {
            _clubDataMigrator = clubDataMigrator;
        }

        [HttpPost]
        public void CreateClub(Club club)
        {
            if (club is null)
            {
                throw new ArgumentNullException(nameof(club));
            }

            _clubDataMigrator.MigrateClub(club);
        }

        [HttpDelete]
        public void DeleteClubs()
        {
            _clubDataMigrator.DeleteClubs();
        }
    }
}