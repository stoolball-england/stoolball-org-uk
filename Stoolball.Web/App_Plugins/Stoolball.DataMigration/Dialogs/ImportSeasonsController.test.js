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
