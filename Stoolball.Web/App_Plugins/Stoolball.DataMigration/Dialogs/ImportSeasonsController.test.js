const seasonResource = require("./ImportSeasonsController");

describe("seasonReducer", () => {
  it("should nest competitionId in a MigratedCompetition", () => {
    const objectUnderTest = seasonResource();
    const result = objectUnderTest.seasonReducer({
      competitionId: 123,
    });

    expect(result.MigratedCompetition.MigratedCompetitionId).toEqual(123);
  });
});

describe("seasonReducer", () => {
  it("should nest competitionRoute in a MigratedCompetition", () => {
    const objectUnderTest = seasonResource();
    const result = objectUnderTest.seasonReducer({
      competitionRoute: "/example-competition",
    });

    expect(result.MigratedCompetition.CompetitionRoute).toEqual(
      "/example-competition"
    );
  });
});

describe("seasonReducer", () => {
  it("should translate teams to a MigratedTeams array", () => {
    const objectUnderTest = seasonResource();
    const result = objectUnderTest.seasonReducer({
      teams: [{ teamId: 123 }],
    });

    expect(result.MigratedTeams.length).toEqual(1);
  });
});

describe("seasonReducer", () => {
  it("should translate teamId to MigratedTeamId", () => {
    const objectUnderTest = seasonResource();
    const result = objectUnderTest.seasonReducer({
      teams: [{ teamId: 123 }],
    });

    expect(result.MigratedTeams[0].MigratedTeamId).toEqual(123);
  });
});

describe("seasonReducer", () => {
  it("should put withdrawnDate into a MigratedTeams array", () => {
    const objectUnderTest = seasonResource();
    const result = objectUnderTest.seasonReducer({
      teams: [{ withdrawnDate: "example" }],
    });

    expect(result.MigratedTeams[0].WithdrawnDate).toEqual("example");
  });
});

describe("seasonReducer", () => {
  it("should translate matchTypes to MatchTypes", () => {
    const objectUnderTest = seasonResource();
    const result = objectUnderTest.seasonReducer({
      matchTypes: [1, 2, 3],
    });

    expect(result.MatchTypes).toEqual([1, 2, 3]);
  });
});

describe("seasonReducer", () => {
  it("should translate pointsRules.homePoints to PointsRules.HomePoints", () => {
    const objectUnderTest = seasonResource();
    const result = objectUnderTest.seasonReducer({
      pointsRules: [
        {
          homePoints: 1,
        },
      ],
    });

    expect(result.PointsRules[0].HomePoints).toEqual(1);
  });
});

describe("seasonReducer", () => {
  it("should translate pointsRules.awayPoints to PointsRules.AwayPoints", () => {
    const objectUnderTest = seasonResource();
    const result = objectUnderTest.seasonReducer({
      pointsRules: [
        {
          awayPoints: 1,
        },
      ],
    });

    expect(result.PointsRules[0].AwayPoints).toEqual(1);
  });
});

describe("seasonReducer", () => {
  it("should translate pointsRules.resultType = 1 to PointsRules.MatchResultType = 0", () => {
    const objectUnderTest = seasonResource();
    const result = objectUnderTest.seasonReducer({
      pointsRules: [{ resultType: 1 }],
    });

    expect(result.PointsRules[0].MatchResultType).toBe(0);
  });
});

describe("seasonReducer", () => {
  it("should translate pointsRules.resultType = 2 to PointsRules.MatchResultType = 1", () => {
    const objectUnderTest = seasonResource();
    const result = objectUnderTest.seasonReducer({
      pointsRules: [{ resultType: 2 }],
    });

    expect(result.PointsRules[0].MatchResultType).toBe(1);
  });
});

describe("seasonReducer", () => {
  it("should translate pointsRules.resultType = 3 to PointsRules.MatchResultType = 2", () => {
    const objectUnderTest = seasonResource();
    const result = objectUnderTest.seasonReducer({
      pointsRules: [{ resultType: 3 }],
    });

    expect(result.PointsRules[0].MatchResultType).toBe(2);
  });
});

describe("seasonReducer", () => {
  it("should translate pointsRules.resultType = 5 to PointsRules.MatchResultType = 3", () => {
    const objectUnderTest = seasonResource();
    const result = objectUnderTest.seasonReducer({
      pointsRules: [{ resultType: 5 }],
    });

    expect(result.PointsRules[0].MatchResultType).toBe(3);
  });
});

describe("seasonReducer", () => {
  it("should translate pointsRules.resultType = 6 to PointsRules.MatchResultType = 4", () => {
    const objectUnderTest = seasonResource();
    const result = objectUnderTest.seasonReducer({
      pointsRules: [{ resultType: 6 }],
    });

    expect(result.PointsRules[0].MatchResultType).toBe(4);
  });
});

describe("seasonReducer", () => {
  it("should translate pointsRules.resultType = 7 to PointsRules.MatchResultType = 5", () => {
    const objectUnderTest = seasonResource();
    const result = objectUnderTest.seasonReducer({
      pointsRules: [{ resultType: 7 }],
    });

    expect(result.PointsRules[0].MatchResultType).toBe(5);
  });
});

describe("seasonReducer", () => {
  it("should translate pointsRules.resultType = 8 to PointsRules.MatchResultType = 6", () => {
    const objectUnderTest = seasonResource();
    const result = objectUnderTest.seasonReducer({
      pointsRules: [{ resultType: 8 }],
    });

    expect(result.PointsRules[0].MatchResultType).toBe(6);
  });
});

describe("seasonReducer", () => {
  it("should translate pointsRules.resultType = 9 to PointsRules.MatchResultType = 7", () => {
    const objectUnderTest = seasonResource();
    const result = objectUnderTest.seasonReducer({
      pointsRules: [{ resultType: 9 }],
    });

    expect(result.PointsRules[0].MatchResultType).toBe(7);
  });
});
