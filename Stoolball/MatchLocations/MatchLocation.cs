using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Stoolball.Audit;
using Stoolball.Teams;

namespace Stoolball.MatchLocations
{
    public class MatchLocation : IAuditable
    {
        [Display(Name = "Ground or sports centre")]
        public Guid? MatchLocationId { get; set; }

        [Display(Name = "Pitch or sports hall")]
        [MaxLength(100)]
        public string SecondaryAddressableObjectName { get; set; }

        [Display(Name = "Ground or sports centre name")]
        [MaxLength(100)]
        [Required]
        public string PrimaryAddressableObjectName { get; set; }

        [Display(Name = "Street address")]
        [MaxLength(100)]
        public string StreetDescription { get; set; }

        [Display(Name = "Village or part of town")]
        [MaxLength(35)]
        public string Locality { get; set; }

        [MaxLength(30)]
        [Required]
        public string Town { get; set; }

        [Display(Name = "County")]
        [MaxLength(30)]
        public string AdministrativeArea { get; set; }

        [MaxLength(8)]
        public string Postcode { get; set; }

        /// <summary>
        /// Gets a concatenated version of the address which can be used for sorting addresses
        /// </summary>
        public string SortName()
        {
            return $" {PrimaryAddressableObjectName} {Town} {SecondaryAddressableObjectName}".ToUpperInvariant().Replace(" THE ", string.Empty).TrimStart();
        }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        [Display(Name = "Accuracy of the map")]
        public GeoPrecision? GeoPrecision { get; set; }

        [Display(Name = "Notes")]
        public string MatchLocationNotes { get; set; }
        public List<Team> Teams { get; internal set; } = new List<Team>();

        public string MatchLocationRoute { get; set; }

        public int MemberGroupId { get; set; }
        public string MemberGroupName { get; set; }

        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();

        public Uri EntityUri {
            get { return new Uri($"https://www.stoolball.org.uk/id/match-location/{MatchLocationId}"); }
        }

        /// <summary>
        /// Gets <see cref="SecondaryAddressableObjectName"/> and <see cref="PrimaryAddressableObjectName"/> combined
        /// </summary>
        /// <returns></returns>
        public string Name()
        {
            var text = new StringBuilder(SecondaryAddressableObjectName);
            if (!string.IsNullOrWhiteSpace(PrimaryAddressableObjectName))
            {
                if (text.Length > 0) text.Append(", ");
                text.Append(PrimaryAddressableObjectName);
            }

            return text.ToString();
        }

        /// <summary>
        /// Get the most useful value of locality or town
        /// </summary>
        /// <returns></returns>
        public string LocalityOrTown()
        {
            if (!string.IsNullOrWhiteSpace(Town))
            {
                if (!string.IsNullOrWhiteSpace(Locality) && Town.StartsWith("Near ", StringComparison.OrdinalIgnoreCase))
                {
                    return Locality;
                }

                else
                {
                    return Town;
                }
            }
            else return Locality;
        }

        /// <summary>
        /// Gets the name of the match location and the town it's in
        /// </summary>
        public string NameAndLocalityOrTown()
        {
            var text = new StringBuilder(Name());
            var placeName = LocalityOrTown();
            if (!string.IsNullOrWhiteSpace(placeName))
            {
                text.Append(", ").Append(placeName);
            }

            return text.ToString();
        }

        /// <summary>
        /// Gets the name of the match location, and the town it's in if that's not in the name
        /// </summary>
        public string NameAndLocalityOrTownIfDifferent()
        {
            var name = Name();
            var placeName = LocalityOrTown();
            if (!string.IsNullOrWhiteSpace(placeName) && !name.ToUpperInvariant().Contains(placeName.ToUpperInvariant()))
            {
                return $"{name}, {placeName}";
            }

            return name;
        }

        /// <summary>
        /// Gets a description of the match location suitable for including in metadata or search results
        /// </summary>
        public string Description()
        {
            var description = new StringBuilder(NameAndLocalityOrTown());
            if (Teams?.Count > 0)
            {
                description.Append(" is home to ");
                for (var i = 0; i < Teams.Count; i++)
                {
                    description.Append(Teams[i].TeamName);
                    if (i < (Teams.Count - 2)) { description.Append(", "); };
                    if (i == (Teams.Count - 2)) { description.Append(" and "); };
                }
                description.Append('.');
            }
            else
            {
                description.Append(" is not currently home to any teams.");
            }
            return description.ToString();
        }
    }
}