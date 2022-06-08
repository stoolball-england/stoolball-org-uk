const objectUnderTestFactory = require("../edit-batting-scorecard");

describe("edit-batting-scorecard.js", () => {
  describe("ordinalSuffixOf", () => {
    it("should format ordinals correctly", () => {
      const objectUnderTest = objectUnderTestFactory();

      expect(objectUnderTest.ordinalSuffixOf(1)).toBe("1st");
      expect(objectUnderTest.ordinalSuffixOf(2)).toBe("2nd");
      expect(objectUnderTest.ordinalSuffixOf(3)).toBe("3rd");
      expect(objectUnderTest.ordinalSuffixOf(4)).toBe("4th");
      expect(objectUnderTest.ordinalSuffixOf(5)).toBe("5th");
      expect(objectUnderTest.ordinalSuffixOf(6)).toBe("6th");
      expect(objectUnderTest.ordinalSuffixOf(7)).toBe("7th");
      expect(objectUnderTest.ordinalSuffixOf(8)).toBe("8th");
      expect(objectUnderTest.ordinalSuffixOf(9)).toBe("9th");
      expect(objectUnderTest.ordinalSuffixOf(10)).toBe("10th");
      expect(objectUnderTest.ordinalSuffixOf(11)).toBe("11th");
      expect(objectUnderTest.ordinalSuffixOf(12)).toBe("12th");
      expect(objectUnderTest.ordinalSuffixOf(13)).toBe("13th");
      expect(objectUnderTest.ordinalSuffixOf(14)).toBe("14th");
      expect(objectUnderTest.ordinalSuffixOf(15)).toBe("15th");
      expect(objectUnderTest.ordinalSuffixOf(16)).toBe("16th");
      expect(objectUnderTest.ordinalSuffixOf(17)).toBe("17th");
      expect(objectUnderTest.ordinalSuffixOf(18)).toBe("18th");
      expect(objectUnderTest.ordinalSuffixOf(19)).toBe("19th");
      expect(objectUnderTest.ordinalSuffixOf(20)).toBe("20th");
      expect(objectUnderTest.ordinalSuffixOf(21)).toBe("21st");
      expect(objectUnderTest.ordinalSuffixOf(22)).toBe("22nd");
      expect(objectUnderTest.ordinalSuffixOf(23)).toBe("23rd");
      expect(objectUnderTest.ordinalSuffixOf(24)).toBe("24th");
      expect(objectUnderTest.ordinalSuffixOf(25)).toBe("25th");
    });
  });
});
