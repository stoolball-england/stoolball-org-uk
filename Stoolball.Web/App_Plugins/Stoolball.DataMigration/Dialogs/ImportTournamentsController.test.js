const tournamentResource = require("./ImportTournamentsController");

describe("tournamentReducer", () => {
  it("should translate playerType = 1 to PlayerType = 0", () => {
    const objectUnderTest = tournamentResource();
    const result = objectUnderTest.tournamentReducer({
      playerType: 1,
    });

    expect(result.PlayerType).toBe(0);
  });
});

describe("tournamentReducer", () => {
  it("should translate playerType = 2 to PlayerType = 1", () => {
    const objectUnderTest = tournamentResource();
    const result = objectUnderTest.tournamentReducer({
      playerType: 2,
    });

    expect(result.PlayerType).toBe(1);
  });
});

describe("tournamentReducer", () => {
  it("should translate playerType = 3 to PlayerType = 2", () => {
    const objectUnderTest = tournamentResource();
    const result = objectUnderTest.tournamentReducer({
      playerType: 3,
    });

    expect(result.PlayerType).toBe(2);
  });
});

describe("tournamentReducer", () => {
  it("should translate playerType = 4 to PlayerType = 3", () => {
    const objectUnderTest = tournamentResource();
    const result = objectUnderTest.tournamentReducer({
      playerType: 4,
    });

    expect(result.PlayerType).toBe(3);
  });
});

describe("tournamentReducer", () => {
  it("should translate playerType = 5 to PlayerType = 4", () => {
    const objectUnderTest = tournamentResource();
    const result = objectUnderTest.tournamentReducer({
      playerType: 5,
    });

    expect(result.PlayerType).toBe(4);
  });
});

describe("tournamentReducer", () => {
  it("should translate playerType = 6 to PlayerType = 5", () => {
    const objectUnderTest = tournamentResource();
    const result = objectUnderTest.tournamentReducer({
      playerType: 6,
    });

    expect(result.PlayerType).toBe(5);
  });
});

describe("tournamentReducer", () => {
  it("should translate qualification = 0 to TournamentQualificationType = null", () => {
    const objectUnderTest = tournamentResource();
    const result = objectUnderTest.tournamentReducer({
      qualification: 0,
    });

    expect(result.TournamentQualificationType).toBe(null);
  });
});

describe("tournamentReducer", () => {
  it("should translate qualification = 1 to TournamentQualificationType = 0", () => {
    const objectUnderTest = tournamentResource();
    const result = objectUnderTest.tournamentReducer({
      qualification: 1,
    });

    expect(result.TournamentQualificationType).toBe(0);
  });
});

describe("tournamentReducer", () => {
  it("should translate qualification = 2 to TournamentQualificationType = 1", () => {
    const objectUnderTest = tournamentResource();
    const result = objectUnderTest.tournamentReducer({
      qualification: 2,
    });

    expect(result.TournamentQualificationType).toBe(1);
  });
});

describe("tournamentReducer", () => {
  it("should ignore teamRole = 1 and pass TeamRole = 2", () => {
    const objectUnderTest = tournamentResource();
    const result = objectUnderTest.tournamentReducer({
      teams: [
        {
          teamId: 123,
          teamRole: 1,
        },
      ],
    });

    expect(result.MigratedTeams.filter((x) => x.TeamRole == 2).length).toBe(1);
  });
});

describe("tournamentReducer", () => {
  it("should ignore teamRole = 2 and pass TeamRole = 2", () => {
    const objectUnderTest = tournamentResource();
    const result = objectUnderTest.tournamentReducer({
      teams: [
        {
          teamId: 123,
          teamRole: 2,
        },
      ],
    });

    expect(result.MigratedTeams.filter((x) => x.TeamRole == 2).length).toBe(1);
  });
});
