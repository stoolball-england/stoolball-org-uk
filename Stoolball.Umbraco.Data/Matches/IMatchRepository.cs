﻿using Stoolball.Matches;
using System;
using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.Matches
{
    public interface IMatchRepository
    {
        /// <summary>
        /// Deletes a stoolball match
        /// </summary>
        Task DeleteMatch(Match match, Guid memberKey, string memberName);
    }
}