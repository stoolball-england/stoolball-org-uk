namespace Stoolball.Teams
{
	public enum TeamType
	{
		/// <summary>
		/// A team that plays regularly
		/// </summary>
		Regular,

		/// <summary>
		/// A team that only particular people can join, such as a work team
		/// </summary>
		ClosedGroup,

		/// <summary>
		/// A team selected to represent a league or other group
		/// </summary>
		Representative,

		/// <summary>
		/// A team that plays occasional friendlies or tournaments
		/// </summary>
		Occasional,

		/// <summary>
		/// A one-off team for a single match or tournament
		/// </summary>
		Once,

		/// <summary>
		/// A team made up of pupils from one or more school years
		/// </summary>
		SchoolAgeGroup,

		/// <summary>
		/// A extra-curricular school club, such as an after-school club 
		/// </summary>
		SchoolClub,

		/// <summary> 
		/// Any other type of school team
		/// </summary>
		SchoolOther
	}
}