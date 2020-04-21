namespace Stoolball.Matches
{
	public enum MatchResultType
	{
		/// <summary>
		/// Home team won the game
		/// </summary>
		HomeWin,

		/// <summary>
		/// Away team won the game
		/// </summary>
		AwayWin,

		/// <summary>
		/// Both teams achieved the same score
		/// </summary>
		Tie,

		/// <summary>
		/// Match was called off at the beginning of or during play
		/// </summary>
		Adandoned,

		/// <summary>
		/// Home team won due to forfeit by away team
		/// </summary>
		HomeWinByForfeit,

		/// <summary>
		/// Away team won due to forfeit by home team
		/// </summary>
		AwayWinByForfeit,

		/// <summary>
		/// Match was cancelled before play
		/// </summary>
		Cancelled,

		/// <summary>
		/// Match will be played at a different time
		/// </summary>
		Postponed
	}
}