const teamResource = require("./ImportTeamsController");

describe("teamReducer", () => {
  it("should translate teamType = 0 to TeamType = 0", () => {
    const objectUnderTest = teamResource();
    const result = objectUnderTest.teamReducer({
      teamType: 0,
    });

    expect(result.TeamType).toBe(0);
  });
});

describe("teamReducer", () => {
  it("should translate teamType = 1 to TeamType = 1", () => {
    const objectUnderTest = teamResource();
    const result = objectUnderTest.teamReducer({
      teamType: 1,
    });

    expect(result.TeamType).toBe(1);
  });
});

describe("teamReducer", () => {
  it("should translate teamType = 2 to TeamType = 2", () => {
    const objectUnderTest = teamResource();
    const result = objectUnderTest.teamReducer({
      teamType: 2,
    });

    expect(result.TeamType).toBe(2);
  });
});

describe("teamReducer", () => {
  it("should translate teamType = 3 to TeamType = 3", () => {
    const objectUnderTest = teamResource();
    const result = objectUnderTest.teamReducer({
      teamType: 3,
    });

    expect(result.TeamType).toBe(3);
  });
});

describe("teamReducer", () => {
  it("should translate teamType = 4 to TeamType = 4", () => {
    const objectUnderTest = teamResource();
    const result = objectUnderTest.teamReducer({
      teamType: 4,
    });

    expect(result.TeamType).toBe(4);
  });
});

describe("teamReducer", () => {
  it("should translate teamType = 6 to TeamType = 5", () => {
    const objectUnderTest = teamResource();
    const result = objectUnderTest.teamReducer({
      teamType: 6,
    });

    expect(result.TeamType).toBe(5);
  });
});

describe("teamReducer", () => {
  it("should translate teamType = 7 to TeamType = 6", () => {
    const objectUnderTest = teamResource();
    const result = objectUnderTest.teamReducer({
      teamType: 7,
    });

    expect(result.TeamType).toBe(6);
  });
});

describe("teamReducer", () => {
  it("should translate teamType = 8 to TeamType = 7", () => {
    const objectUnderTest = teamResource();
    const result = objectUnderTest.teamReducer({
      teamType: 8,
    });

    expect(result.TeamType).toBe(7);
  });
});
