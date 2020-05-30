namespace Stoolball.Matches
{
	/// <summary>
	/// Enum determining what kind of stoolball match is being represented
	/// </summary>
	public enum MatchType
	{
		/// <summary>
		/// A match in a league
		/// </summary>
		LeagueMatch,

		/// <summary>
		/// A tournament, which is a group of other matches
		/// </summary>
		Tournament,

		/// <summary>
		/// A match which is part of a tournament
		/// </summary>
		TournamentMatch,

		/// <summary>
		/// A practice session, rather than a real match
		/// </summary>
		Practice,

		/// <summary>
		/// A one-off friendly match
		/// </summary>
		FriendlyMatch,

		/// <summary>
		/// A knock-out cup match, in which the loser is usually eliminated from the competition
		/// </summary>
		KnockoutMatch
	}
}