using System;

namespace Stoolball.Web.Matches
{
    public class MatchAwardViewModel
    {
        public Guid? MatchAwardId { get; set; }

        /// <remarks>
        /// Avoid the terms "identity" and "name" and use the term "search" to try to avoid contact/password managers adding pop-ups.
        /// </remarks>
        public string PlayerSearch { get; set; }

        public Guid? TeamId { get; set; }
        public string Reason { get; set; }
    }
}