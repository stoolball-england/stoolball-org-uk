const relatedItemsManager = require("../related-items");

describe("relatedItemsManager", () => {
  it("should replace all instances of {{value}} with suggestion.value", () => {
    const objectUnderTest = relatedItemsManager();
    const result = objectUnderTest.populateTemplate(
      "<p>{{value}} and {{ value }}</p>",
      {
        value: "example",
      }
    );

    expect(result).toBe("<p>example and example</p>");
  });

  it("should replace all instances of {{data}} with suggestion.data if suggestion.data is a string", () => {
    const objectUnderTest = relatedItemsManager();
    const result = objectUnderTest.populateTemplate(
      "<p>{{data}} and {{ data}}</p>",
      {
        data: "example",
      }
    );

    expect(result).toBe("<p>example and example</p>");
  });

  it("should replace all instances of {{data}} with suggestion.data.data if suggestion.data is an object", () => {
    const objectUnderTest = relatedItemsManager();
    const result = objectUnderTest.populateTemplate(
      "<p>{{data }} and {{ data }}</p>",
      {
        data: { data: "example" },
      }
    );

    expect(result).toBe("<p>example and example</p>");
  });

  it("should replace all instances of {{data.anyCustomProperty}} with the value of suggestion.data.anyCustomProperty", () => {
    const objectUnderTest = relatedItemsManager();
    const result = objectUnderTest.populateTemplate(
      "<p>{{ data.anyCustomProperty }} and {{ data.anyCustomProperty }}</p>",
      {
        data: { anyCustomProperty: "example" },
      }
    );

    expect(result).toBe("<p>example and example</p>");
  });

  it("should replace all instances of {{create}} with Yes if suggestion.data.create is truthy", () => {
    const objectUnderTest = relatedItemsManager();
    const result = objectUnderTest.populateTemplate(
      "<p>{{create}} and {{ create  }}</p>",
      {
        data: { create: true },
      }
    );

    expect(result).toBe("<p>Yes and Yes</p>");
  });

  it("should replace all instances of {{create}} with No if suggestion.data.create is falsy", () => {
    const objectUnderTest = relatedItemsManager();
    const result = objectUnderTest.populateTemplate(
      "<p>{{create}} and {{create}}</p>",
      {
        data: { create: false },
      }
    );

    expect(result).toBe("<p>No and No</p>");
  });

  it("should replace all instances of {{id}} with a new UUID", () => {
    const objectUnderTest = relatedItemsManager();
    const result = objectUnderTest.populateTemplate(
      "<p>{{id}} and {{ id }}</p>",
      {}
    );

    expect(result).toMatch(
      /<p>\b[0-9a-f]{8}\b-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-\b[0-9a-f]{12}\b and \b[0-9a-f]{8}\b-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-\b[0-9a-f]{12}\b<\/p>/
    );
  });
});
