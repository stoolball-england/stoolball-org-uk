namespace Stoolball.Matches
{
    public enum DismissalType
    {
        /// <summary>
        /// Player took part in the match but did not bat
        /// </summary>	 
        DidNotBat,

        /// <summary>
        /// Player batted and was undefeated at the end of the team's innings
        /// </summary>	 
        NotOut,

        /// <summary>
        /// Player retired for a reason other than an injury obtained in the match
        /// </summary>	 
        Retired,

        /// <summary>
        /// Player retired due to an injury obtained in the match
        /// </summary>	 
        RetiredHurt,

        /// <summary>
        /// Player was out, caught by the bowler
        /// </summary>	 
        CaughtAndBowled,

        /// <summary>
        /// Player was out, run-out
        /// </summary>	 
        RunOut,

        /// <summary>
        /// Player was out, body before wicket
        /// </summary>	 
        BodyBeforeWicket,

        /// <summary>
        /// Player was out having hit the ball twice deliberately
        /// </summary>	 
        HitTheBallTwice,

        /// <summary>
        /// Player was out, timed out
        /// </summary>	 
        TimedOut,

        /// <summary>
        /// Player was out, caught
        /// </summary>	 
        Caught,

        /// <summary>
        /// Player was out, bowled
        /// </summary>	 
        Bowled
    }
}
