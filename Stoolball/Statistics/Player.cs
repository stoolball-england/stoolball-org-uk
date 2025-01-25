using System;
using System.Collections.Generic;
using System.Linq;
using Stoolball.Logging;

namespace Stoolball.Statistics
{
    public class Player : IAuditable
    {
        private string _primaryName = string.Empty;
        private string? _preferredName = string.Empty;
        private readonly List<string> _alternativeNames = new();

        public Guid? PlayerId { get; set; }

        public string? PreferredName {
            get => _preferredName;
            set {
                _primaryName = string.Empty;
                _preferredName = value;
            }
        }

        private class CandidateName
        {
            public required string Name { get; set; }
            public int TotalMatches { get; set; }
            public DateTimeOffset? LastPlayed { get; set; }
            public int Weighting { get; set; } = 1;
        }

        /// <summary>
        /// Gets the preferred name for a player based on all the <see cref="PlayerIdentities"/>
        /// </summary>
        public string PlayerName()
        {
            InitialisePreferredAndAlternativeNames();
            return _primaryName;
        }

        /// <summary>
        /// Select a preferred name and a list of alternative names from the <see cref="PlayerIdentities"/>
        /// </summary>
        private void InitialisePreferredAndAlternativeNames()
        {
            if (string.IsNullOrEmpty(_primaryName))
            {
                _alternativeNames.Clear();
                var names = CombineDuplicateNames();
                var groups = DivideNamesIntoGroups(names);
                for (var i = 0; i < groups.Count; i++)
                {
                    groups[i] = AssignWeightingBasedOnLastPlayed(groups[i]);
                    groups[i] = OrderByWeightedTotalMatches(groups[i]);
                }
                var recombined = groups[0].Concat(groups[1]).Concat(groups[2]).ToList();

                if (!string.IsNullOrEmpty(_preferredName))
                {
                    _primaryName = _preferredName;
                    var identityMatchingPreferredName = recombined.SingleOrDefault(name => name.Name == _preferredName);
                    if (identityMatchingPreferredName is not null) { recombined.Remove(identityMatchingPreferredName); }
                }
                else
                {
                    if (recombined.Count > 0)
                    {
                        _primaryName = recombined[0].Name;
                        recombined.RemoveAt(0);
                    }
                }
                if (recombined.Count > 0)
                {
                    _alternativeNames.AddRange(recombined.Select(x => x.Name));
                }
            };
        }

        /// <summary>
        /// Order a list of names by the total matches played * the weighting
        /// </summary>
        /// <param name="group"></param>
        private List<CandidateName> OrderByWeightedTotalMatches(List<CandidateName> group)
        {
            return group.OrderByDescending(x => x.TotalMatches * x.Weighting).ToList();
        }

        /// <summary>
        /// Assign the names most recently recorded a higher weighting
        /// </summary>
        private List<CandidateName> AssignWeightingBasedOnLastPlayed(List<CandidateName> group)
        {
            var recent = group.Where(x => x.LastPlayed.HasValue).OrderByDescending(x => x.LastPlayed).Take(4);
            var weighting = 6;
            var lastPlayed = DateTimeOffset.MaxValue;
            foreach (var candidate in recent)
            {
                if (candidate.LastPlayed < lastPlayed)
                {
                    weighting--;
                }
                candidate.Weighting = weighting;
                lastPlayed = candidate.LastPlayed!.Value;
            }
            return group;
        }

        /// <summary>
        /// Complete names are better. Divide into three groups - two or more names, one name + initial, one name
        /// </summary> 
        private List<List<CandidateName>> DivideNamesIntoGroups(IEnumerable<CandidateName> names)
        {
            var group1 = new List<CandidateName>();
            var group2 = new List<CandidateName>();
            var group3 = new List<CandidateName>();

            foreach (var name in names)
            {
                var firstSpace = name.Name.IndexOf(" ");
                if (firstSpace > -1)
                {
                    var lastSpace = name.Name.LastIndexOf(" ");
                    if (firstSpace == lastSpace && (lastSpace == name.Name.Length - 2 || lastSpace == 1))
                    {
                        group2.Add(name);
                    }
                    else
                    {
                        group1.Add(name);
                    }
                }
                else
                {
                    group3.Add(name);
                }
            }

            return new List<List<CandidateName>> { group1, group2, group3 };
        }

        /// <summary>
        /// Combine duplicate names, aggregating total matches and preferring the most recent usage of the name
        /// </summary>
        private IEnumerable<CandidateName> CombineDuplicateNames()
        {
            var names = new Dictionary<string, CandidateName>();
            foreach (var identity in PlayerIdentities)
            {
                if (string.IsNullOrEmpty(identity.PlayerIdentityName)) { continue; }
                if (!names.ContainsKey(identity.PlayerIdentityName))
                {
                    names.Add(identity.PlayerIdentityName, new CandidateName { Name = identity.PlayerIdentityName, TotalMatches = identity.TotalMatches ?? 0, LastPlayed = identity.LastPlayed });
                }
                else
                {
                    if (identity.TotalMatches.HasValue)
                    {
                        names[identity.PlayerIdentityName].TotalMatches += identity.TotalMatches.Value;
                    }
                    if (!names[identity.PlayerIdentityName].LastPlayed.HasValue || (identity.LastPlayed.HasValue && identity.LastPlayed > names[identity.PlayerIdentityName].LastPlayed))
                    {
                        names[identity.PlayerIdentityName].LastPlayed = identity.LastPlayed;
                    }
                }
            }
            return names.Values;
        }

        /// <summary>
        /// Gets whether another player has an identity on the same team as any of this player's identities.
        /// </summary>
        /// <param name="otherPlayer">The player to compare.</param>
        /// <returns><c>true</c> if at least one identity is on the same team as one identity; <c>false</c> otherwise.</returns>
        public bool IsOnTheSameTeamAs(Player otherPlayer) => PlayerIdentities.Select(pi => pi.Team?.TeamId).Intersect(otherPlayer.PlayerIdentities.Select(pi => pi.Team?.TeamId)).Any();


        public string? PlayerRoute { get; set; }

        public Guid? MemberKey { get; set; }

        public PlayerIdentityList PlayerIdentities { get; internal set; } = new PlayerIdentityList();

        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();

        public Uri EntityUri {
            get { return new Uri($"https://www.stoolball.org.uk/id/player/{PlayerId}"); }
        }

        /// <summary>
        /// Gets the alternative names for a player with multiple player identities where the names differ
        /// </summary>
        /// <returns>A list of 0 or more names</returns>
        public List<string> AlternativeNames()
        {
            InitialisePreferredAndAlternativeNames();
            return _alternativeNames;
        }
    }
}
