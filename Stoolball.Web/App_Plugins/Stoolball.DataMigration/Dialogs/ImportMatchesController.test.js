const matchResource = require("./ImportMatchesController");

describe("matchReducer", () => {
  it("should translate homeBatFirst = true to InningsOrderIsKnown = true", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      homeBatFirst: true,
    });

    expect(result.InningsOrderIsKnown).toBe(true);
  });
});

describe("matchReducer", () => {
  it("should translate homeBatFirst = false to InningsOrderIsKnown = true", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      homeBatFirst: false,
    });

    expect(result.InningsOrderIsKnown).toBe(true);
  });
});

describe("matchReducer", () => {
  it("should translate homeBatFirst = null to InningsOrderIsKnown = false", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      homeBatFirst: null,
    });

    expect(result.InningsOrderIsKnown).toBe(false);
  });
});

describe("matchReducer", () => {
  it("should translate homeBatFirst = true to InningsOrderInMatch = [1,2]", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      homeBatFirst: true,
    });

    expect(result.MigratedMatchInnings[0].InningsOrderInMatch).toBe(1);
    expect(result.MigratedMatchInnings[1].InningsOrderInMatch).toBe(2);
  });
});

describe("matchReducer", () => {
  it("should translate homeBatFirst = false to InningsOrderInMatch = [2,1]", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      homeBatFirst: false,
    });

    expect(result.MigratedMatchInnings[0].InningsOrderInMatch).toBe(2);
    expect(result.MigratedMatchInnings[1].InningsOrderInMatch).toBe(1);
  });
});

describe("matchReducer", () => {
  it("should translate homeBatFirst = null to InningsOrderInMatch = [1,2]", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      homeBatFirst: null,
    });

    expect(result.MigratedMatchInnings[0].InningsOrderInMatch).toBe(1);
    expect(result.MigratedMatchInnings[1].InningsOrderInMatch).toBe(2);
  });
});

describe("matchReducer", () => {
  it("puts the home team first in a MigratedMatchInnings array", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      teams: [
        {
          teamId: 123,
          teamRole: 1,
        },
      ],
    });

    expect(result.MigratedMatchInnings[0].MigratedTeamId).toBe(123);
  });
});

describe("matchReducer", () => {
  it("puts the away team second in a MigratedMatchInnings array", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      teams: [
        {
          teamId: 123,
          teamRole: 2,
        },
      ],
    });

    expect(result.MigratedMatchInnings[1].MigratedTeamId).toBe(123);
  });
});

describe("matchReducer", () => {
  it("should translate teamRole = 1 to TeamRole = 0", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      teams: [
        {
          teamId: 123,
          teamRole: 1,
        },
      ],
    });

    expect(result.MigratedTeams.filter((x) => x.TeamRole == 0).length).toBe(1);
  });
});

describe("matchReducer", () => {
  it("should translate teamRole = 2 to TeamRole = 1", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      teams: [
        {
          teamId: 123,
          teamRole: 2,
        },
      ],
    });

    expect(result.MigratedTeams.filter((x) => x.TeamRole == 1).length).toBe(1);
  });
});

describe("matchReducer", () => {
  it("should translate tossWonBy = 1 to home team won toss", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      tossWonBy: 1,
      teams: [
        {
          teamId: 123,
          teamRole: 1,
        },
      ],
    });

    expect(result.MigratedTeams.filter((x) => x.TeamRole == 0)[0].WonToss).toBe(
      true
    );
  });
});

describe("matchReducer", () => {
  it("should translate tossWonBy = 1 to away team lost toss", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      tossWonBy: 1,
      teams: [
        {
          teamId: 123,
          teamRole: 2,
        },
      ],
    });

    expect(result.MigratedTeams.filter((x) => x.TeamRole == 1)[0].WonToss).toBe(
      false
    );
  });
});

describe("matchReducer", () => {
  it("should translate tossWonBy = 2 to home team lost toss", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      tossWonBy: 2,
      teams: [
        {
          teamId: 123,
          teamRole: 1,
        },
      ],
    });

    expect(result.MigratedTeams.filter((x) => x.TeamRole == 0)[0].WonToss).toBe(
      false
    );
  });
});

describe("matchReducer", () => {
  it("should translate tossWonBy = 2 to away team won toss", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      tossWonBy: 2,
      teams: [
        {
          teamId: 123,
          teamRole: 2,
        },
      ],
    });

    expect(result.MigratedTeams.filter((x) => x.TeamRole == 1)[0].WonToss).toBe(
      true
    );
  });
});

describe("matchReducer", () => {
  it("should translate tossWonBy = null to home team won toss = null", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      tossWonBy: null,
      teams: [
        {
          teamId: 123,
          teamRole: 1,
        },
      ],
    });

    expect(result.MigratedTeams.filter((x) => x.TeamRole == 0)[0].WonToss).toBe(
      null
    );
  });
});

describe("matchReducer", () => {
  it("should translate tossWonBy = null to away team won toss = null", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      tossWonBy: null,
      teams: [
        {
          teamId: 123,
          teamRole: 2,
        },
      ],
    });

    expect(result.MigratedTeams.filter((x) => x.TeamRole == 1)[0].WonToss).toBe(
      null
    );
  });
});

describe("matchReducer", () => {
  it("should translate resultType = null to MatchResultType = null", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      resultType: null,
    });

    expect(result.MatchResultType).toBe(null);
  });
});

describe("matchReducer", () => {
  it("should translate resultType = 1 to MatchResultType = 0", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      resultType: 1,
    });

    expect(result.MatchResultType).toBe(0);
  });
});

describe("matchReducer", () => {
  it("should translate resultType = 2 to MatchResultType = 1", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      resultType: 2,
    });

    expect(result.MatchResultType).toBe(1);
  });
});
describe("matchReducer", () => {
  it("should translate resultType = 3 to MatchResultType = 2", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      resultType: 3,
    });

    expect(result.MatchResultType).toBe(2);
  });
});
describe("matchReducer", () => {
  it("should translate resultType = 5 to MatchResultType = 3", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      resultType: 5,
    });

    expect(result.MatchResultType).toBe(3);
  });
});
describe("matchReducer", () => {
  it("should translate resultType = 6 to MatchResultType = 4", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      resultType: 6,
    });

    expect(result.MatchResultType).toBe(4);
  });
});
describe("matchReducer", () => {
  it("should translate resultType = 7 to MatchResultType = 5", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      resultType: 7,
    });

    expect(result.MatchResultType).toBe(5);
  });
});
describe("matchReducer", () => {
  it("should translate resultType = 8 to MatchResultType = 6", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      resultType: 8,
    });

    expect(result.MatchResultType).toBe(6);
  });
});
describe("matchReducer", () => {
  it("should translate resultType = 9 to MatchResultType = 7", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      resultType: 9,
    });

    expect(result.MatchResultType).toBe(7);
  });
});

describe("matchReducer", () => {
  it("should translate playerType = 1 to PlayerType = 0", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      playerType: 1,
    });

    expect(result.PlayerType).toBe(0);
  });
});

describe("matchReducer", () => {
  it("should translate playerType = 2 to PlayerType = 1", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      playerType: 2,
    });

    expect(result.PlayerType).toBe(1);
  });
});

describe("matchReducer", () => {
  it("should translate playerType = 3 to PlayerType = 2", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      playerType: 3,
    });

    expect(result.PlayerType).toBe(2);
  });
});

describe("matchReducer", () => {
  it("should translate playerType = 4 to PlayerType = 3", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      playerType: 4,
    });

    expect(result.PlayerType).toBe(3);
  });
});

describe("matchReducer", () => {
  it("should translate playerType = 5 to PlayerType = 4", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      playerType: 5,
    });

    expect(result.PlayerType).toBe(4);
  });
});

describe("matchReducer", () => {
  it("should translate playerType = 6 to PlayerType = 5", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      playerType: 6,
    });

    expect(result.PlayerType).toBe(5);
  });
});

describe("tournamentReducer", () => {
  it("should translate playerType = 1 to PlayerType = 0", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.tournamentReducer({
      playerType: 1,
    });

    expect(result.PlayerType).toBe(0);
  });
});

describe("tournamentReducer", () => {
  it("should translate playerType = 2 to PlayerType = 1", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.tournamentReducer({
      playerType: 2,
    });

    expect(result.PlayerType).toBe(1);
  });
});

describe("tournamentReducer", () => {
  it("should translate playerType = 3 to PlayerType = 2", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.tournamentReducer({
      playerType: 3,
    });

    expect(result.PlayerType).toBe(2);
  });
});

describe("tournamentReducer", () => {
  it("should translate playerType = 4 to PlayerType = 3", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.tournamentReducer({
      playerType: 4,
    });

    expect(result.PlayerType).toBe(3);
  });
});

describe("tournamentReducer", () => {
  it("should translate playerType = 5 to PlayerType = 4", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.tournamentReducer({
      playerType: 5,
    });

    expect(result.PlayerType).toBe(4);
  });
});

describe("tournamentReducer", () => {
  it("should translate playerType = 6 to PlayerType = 5", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.tournamentReducer({
      playerType: 6,
    });

    expect(result.PlayerType).toBe(5);
  });
});

describe("tournamentReducer", () => {
  it("should translate qualification = 0 to TournamentQualificationType = null", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.tournamentReducer({
      qualification: 0,
    });

    expect(result.TournamentQualificationType).toBe(null);
  });
});

describe("tournamentReducer", () => {
  it("should translate qualification = 1 to TournamentQualificationType = 0", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.tournamentReducer({
      qualification: 1,
    });

    expect(result.TournamentQualificationType).toBe(0);
  });
});

describe("tournamentReducer", () => {
  it("should translate qualification = 2 to TournamentQualificationType = 1", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.tournamentReducer({
      qualification: 2,
    });

    expect(result.TournamentQualificationType).toBe(1);
  });
});

describe("tournamentReducer", () => {
  it("should ignore teamRole = 1 and pass TeamRole = 2", () => {
    const objectUnderTest = matchResource();
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
    const objectUnderTest = matchResource();
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
