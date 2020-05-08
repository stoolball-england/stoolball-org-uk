const matchResource = require("./ImportMatchesController");

describe("matchResource", () => {
  it("should translate homeBatFirst = true to InningsOrderIsKnown = true", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      homeBatFirst: true,
    });

    expect(result.InningsOrderIsKnown).toBe(true);
  });
});

describe("matchResource", () => {
  it("should translate homeBatFirst = false to InningsOrderIsKnown = true", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      homeBatFirst: false,
    });

    expect(result.InningsOrderIsKnown).toBe(true);
  });
});

describe("matchResource", () => {
  it("should translate homeBatFirst = null to InningsOrderIsKnown = false", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      homeBatFirst: null,
    });

    expect(result.InningsOrderIsKnown).toBe(false);
  });
});

describe("matchResource", () => {
  it("should translate homeBatFirst = true to InningsOrderInMatch = [1,2]", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      homeBatFirst: true,
    });

    expect(result.MigratedMatchInnings[0].InningsOrderInMatch).toBe(1);
    expect(result.MigratedMatchInnings[1].InningsOrderInMatch).toBe(2);
  });
});

describe("matchResource", () => {
  it("should translate homeBatFirst = false to InningsOrderInMatch = [2,1]", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      homeBatFirst: false,
    });

    expect(result.MigratedMatchInnings[0].InningsOrderInMatch).toBe(2);
    expect(result.MigratedMatchInnings[1].InningsOrderInMatch).toBe(1);
  });
});

describe("matchResource", () => {
  it("should translate homeBatFirst = null to InningsOrderInMatch = [1,2]", () => {
    const objectUnderTest = matchResource();
    const result = objectUnderTest.matchReducer({
      homeBatFirst: null,
    });

    expect(result.MigratedMatchInnings[0].InningsOrderInMatch).toBe(1);
    expect(result.MigratedMatchInnings[1].InningsOrderInMatch).toBe(2);
  });
});

describe("matchResource", () => {
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

describe("matchResource", () => {
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

describe("matchResource", () => {
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

describe("matchResource", () => {
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

describe("matchResource", () => {
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

describe("matchResource", () => {
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

describe("matchResource", () => {
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

describe("matchResource", () => {
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
