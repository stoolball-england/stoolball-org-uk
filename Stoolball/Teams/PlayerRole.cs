namespace Stoolball.Teams
{
	public enum PlayerRole
	{
		/// <summary>
		/// An ordinary player rather than a type of extra
		/// </summary>
		Player,

		/// <summary>
		/// No balls is a special player representing extras bowled
		/// </summary>
		NoBalls,

		/// <summary>
		/// Wides is a special player representing extras bowled
		/// </summary>
		Wides,

		/// <summary>
		/// Byes is a special player representing extras conceded
		/// </summary>
		Byes,

		/// <summary>
		/// Bonus runs is a special player representing any bonus or penalty runs
		/// </summary>
		BonusRuns
	}
}
