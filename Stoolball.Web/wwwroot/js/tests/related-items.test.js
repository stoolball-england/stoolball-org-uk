const relatedItemsEditor = require("../related-items");

describe("relatedItemsManager.populateTemplate", () => {
  it("should replace all instances of {{value}} with suggestion.value", () => {
    const objectUnderTest = relatedItemsEditor();
    const result = objectUnderTest.populateTemplate(
      "<p>{{value}} and {{ value }}</p>",
      {
        value: "example",
      }
    );

    expect(result).toBe("<p>example and example</p>");
  });

  it("should replace all instances of {{data}} with suggestion.data if suggestion.data is a string", () => {
    const objectUnderTest = relatedItemsEditor();
    const result = objectUnderTest.populateTemplate(
      "<p>{{data}} and {{ data}}</p>",
      {
        data: "example",
      }
    );

    expect(result).toBe("<p>example and example</p>");
  });

  it("should replace all instances of {{data}} with suggestion.data.data if suggestion.data is an object", () => {
    const objectUnderTest = relatedItemsEditor();
    const result = objectUnderTest.populateTemplate(
      "<p>{{data }} and {{ data }}</p>",
      {
        data: { data: "example" },
      }
    );

    expect(result).toBe("<p>example and example</p>");
  });

  it("should replace all instances of {{data.anyCustomProperty}} with the value of suggestion.data.anyCustomProperty", () => {
    const objectUnderTest = relatedItemsEditor();
    const result = objectUnderTest.populateTemplate(
      "<p>{{ data.anyCustomProperty }} and {{ data.anyCustomProperty }}</p>",
      {
        data: { anyCustomProperty: "example" },
      }
    );

    expect(result).toBe("<p>example and example</p>");
  });

  it("should replace all instances of {{create}} with Yes if suggestion.data.create is truthy", () => {
    const objectUnderTest = relatedItemsEditor();
    const result = objectUnderTest.populateTemplate(
      "<p>{{create}} and {{ create  }}</p>",
      {
        data: { create: true },
      }
    );

    expect(result).toBe("<p>Yes and Yes</p>");
  });

  it("should replace all instances of {{create}} with No if suggestion.data.create is falsy", () => {
    const objectUnderTest = relatedItemsEditor();
    const result = objectUnderTest.populateTemplate(
      "<p>{{create}} and {{create}}</p>",
      {
        data: { create: false },
      }
    );

    expect(result).toBe("<p>No and No</p>");
  });

  it("should replace all instances of {{id}} with a new UUID", () => {
    const objectUnderTest = relatedItemsEditor();
    const result = objectUnderTest.populateTemplate(
      "<p>{{id}} and {{ id }}</p>",
      {}
    );

    expect(result).toMatch(
      /<p>\b[0-9a-f]{8}\b-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-\b[0-9a-f]{12}\b and \b[0-9a-f]{8}\b-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-\b[0-9a-f]{12}\b<\/p>/
    );
  });
});

describe("relatedItemsManager.resetEmpty", () => {
  it("should set the empty class if there are no data rows", () => {
    const objectUnderTest = relatedItemsEditor();

    editor = {
      querySelectorAll: () => [],
      classList: {
        add: jest.fn(),
        remove: jest.fn(),
      },
    };
    objectUnderTest.resetEmpty(editor);

    expect(editor.classList.add).toBeCalledWith("related-items__empty");
    expect(editor.classList.remove).not.toBeCalled();
  });

  it("should remove the empty class if there are data rows", () => {
    const objectUnderTest = relatedItemsEditor();

    editor = {
      querySelectorAll: () => [{}],
      classList: {
        add: jest.fn(),
        remove: jest.fn(),
      },
    };
    objectUnderTest.resetEmpty(editor);

    expect(editor.classList.add).not.toBeCalled();
    expect(editor.classList.remove).toBeCalledWith("related-items__empty");
  });
});

describe("relatedItemsManager.resetAutocompleteParams", () => {
  it("given a table row, should append selected ids in all sibling rows to an existing url using a 'not' parameter", () => {
    document.body.innerHTML = `<table class="table table-hover related-items">
      <tbody>
        <tr class="related-item__selected">
          <td class="related-item__delete related-item__selected__section">
            <input class="related-item__data related-item__id" type="hidden" value="80357a92-c521-4c77-b07f-5ddecc46b7ba" />
          </td>
        </tr>
        <tr class="related-item__selected">
            <td class="related-item__delete related-item__selected__section">
              <input class="related-item__data related-item__id" type="hidden" value="c53eae50-d580-4236-a5ed-a2de869acd91" />
            </td>
        </tr>
      </tbody>
  </table>`;

    const objectUnderTest = relatedItemsEditor();

    const firstRow = document.querySelector("tr");

    // DO NOT expect the alternative format of not%5B%5D=80357a92-c521-4c77-b07f-5ddecc46b7ba
    // which is valid, but not understood by [FromQuery] in .NET 5
    const expectedSuffix =
      "not=80357a92-c521-4c77-b07f-5ddecc46b7ba&not=c53eae50-d580-4236-a5ed-a2de869acd91";
    const urlWithQuery = "https://example.org/path?some=query";
    const result1 = objectUnderTest.resetAutocompleteParams(
      firstRow,
      urlWithQuery
    );

    const urlWithoutQuery = "https://example.org/path";
    const result2 = objectUnderTest.resetAutocompleteParams(
      firstRow,
      urlWithoutQuery
    );

    expect(result1).toBe(urlWithQuery + "&" + expectedSuffix);
    expect(result2).toBe(urlWithoutQuery + "?" + expectedSuffix);
  });
});
