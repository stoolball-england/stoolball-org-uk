﻿using System;
using Stoolball.Statistics;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Statistics.Models
{
    public class LinkedPlayersViewModel : BaseViewModel
    {
        public LinkedPlayersViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }

        public Player? Player { get; set; }

        public PlayerIdentity? ContextIdentity { get; set; }

        public string PreferredNextRoute { get; set; } = Constants.Pages.AccountUrl;

        public string LinkedByHeading { get; set; } = "Linked by";
        public string LinkedByMemberLabel { get; set; } = PlayerIdentityLinkedBy.Member.ToString();
        public string LinkedByClubOrTeamLabel { get; set; } = PlayerIdentityLinkedBy.ClubOrTeam.ToString();
        public string LinkedByStoolballEnglandLabel { get; set; } = PlayerIdentityLinkedBy.StoolballEngland.ToString();

        public bool CanUnlinkIdentitiesLinkedByMember { get; set; }
        public bool CanUnlinkIdentitiesLinkedByClubOrTeam { get; set; }
        public Guid? AddIdentitiesFromTeamId { get; set; }
        public bool CanRemoveFinalIdentity { get; set; }
    }
}