﻿using Stoolball.Matches;

namespace Stoolball.Web.Matches.Models
{
    /// <remarks>
    /// This exists solely to collect details of an <see cref="Over"/> without having 'Identity' or 'Name' in the generated field name, 
    /// which encourages contact/password managers to add their own pop-ups on the field.
    /// </remarks>
    public class OverViewModel : Over
    {
        public string? BowledBy { get; set; }
    }
}