import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";
import { HtmlValidate, type ConfigData } from "html-validate";

const htmlValidate = new HtmlValidate({
  root: true,
  extends: ["html-validate:recommended", "html-validate:document"],
  rules: {
    "missing-doctype": "off", // the doctype is not part of document.documentElement.outerHTML
    "no-inline-style": "off", // scripts often use inline styles, eg for showing/hiding elements
    "no-trailing-whitespace": "off", // scripts often add markup with trailing whitespace
    "attribute-boolean-style": "off", // the browser normalises boolean attributes
    "attribute-empty-style": "off", // the browser normalises empty attributes
    "void-style": "off", // the browser serialises void elements its own way
    "no-self-closing": "off", // the browser serialises void elements its own way
    "script-type": "off", // because it fails Smidge-generated <script type="text/javascript"> tags for including the type
    "require-sri": "off", // because it requires SRI for local resources (even with the crossorigin setting because the html-validate project admits detection isn't good enough)
    "long-title": "off", // some competition and team names are genuinely very long!
    "form-dup-name": [
      "error",
      { shared: ["radio", "checkbox", "button", "reset", "submit"] }, // a group of checkboxes can share a name, eg Umbraco Forms checkbox lists
    ],
  },
});

/**
 * Validates the current DOM of the page using html-validate.
 * @param page The page to validate.
 * @param config Optional html-validate config merged over the defaults, eg to turn off a rule for one page.
 */
export async function expectValidHtml(page: Page, config: ConfigData = {}) {
  const html = await page.evaluate(() => document.documentElement.outerHTML);
  const report = await htmlValidate.validateString(html, config);

  const errors = report.results.flatMap((result) =>
    result.messages
      .filter(
        // Visual Studio Browser Link inserts this element when running locally. It's not present in production.
        (message) =>
          !(
            message.ruleId === "valid-id" &&
            message.selector === "#__browserLink_initializationData"
          ),
      )
      .map(
        (message) =>
          `${message.ruleId}: ${message.message} (${message.selector ?? `line ${message.line}`})`,
      ),
  );
  expect(
    errors,
    `${errors.length} html-validate error${errors.length === 1 ? "" : "s"}`,
  ).toEqual([]);
}

/**
 * Reads the position and ranked value of each row in a statistics table.
 * @param page The page containing the statistics table.
 * @param columns The headings of the columns that decide whether two rows are equal. Defaults to the last column.
 */
async function readStatisticsRows(page: Page, columns?: string[]) {
  return await page.locator("table.statistics").evaluate((table, columns) => {
    const headings = [...table.querySelectorAll("thead th")].map((th) => th.textContent?.trim() ?? "");
    const indexes = columns ? columns.map((column) => headings.indexOf(column)) : [headings.length - 1];
    if (indexes.some((index) => index < 0)) {
      throw new Error(`Columns ${JSON.stringify(columns)} not found in ${JSON.stringify(headings)}`);
    }
    return [...table.querySelectorAll("tbody tr")].map((row) => {
      const cells = row.querySelectorAll("td");
      const value = indexes
        .map((index) => {
          const cell = cells[index].cloneNode(true) as HTMLElement;
          cell.querySelector(".table-as-cards__label")?.remove();
          return cell.textContent?.trim() ?? "";
        })
        .join(" ");
      return { position: cells[0].textContent?.trim() ?? "", value };
    });
  }, columns);
}

/**
 * Checks the positions in a statistics table: a row whose displayed value is the same as the
 * row above shows "=", and any other row shows its position number.
 * @param page The page containing the statistics table.
 * @param columns The headings of the columns that decide whether two rows are equal. Defaults to the last column.
 */
export async function expectEqualStatisticsShareAPosition(page: Page, columns?: string[]) {
  const rows = await readStatisticsRows(page, columns);

  expect(rows.length, "statistics table has rows").toBeGreaterThan(0);
  rows.forEach((row, i) => {
    if (i > 0 && row.value === rows[i - 1].value) {
      expect(row.position, `row ${i + 1} equals the row above`).toBe("=");
    } else {
      expect(row.position, `row ${i + 1} differs from the row above`).toMatch(/^\d+$/);
    }
  });
}

/**
 * Checks that positions in a statistics table continue from one page to the next: the first row
 * on the next page shows "=" if its displayed value is the same as the last row on this page, and
 * otherwise shows the next position number.
 * @param page The first page of the statistics table. It navigates to the next page.
 * @param columns The headings of the columns that decide whether two rows are equal. Defaults to the last column.
 */
export async function expectPositionsContinueOnNextPage(page: Page, columns?: string[]) {
  const firstPage = await readStatisticsRows(page, columns);
  const nextPage = page.getByRole("link", { name: "Next page" });
  test.skip((await nextPage.count()) === 0, "There is only one page of results");

  await nextPage.click();
  await expect(page).toHaveURL(/[?&]page=2\b/);
  const secondPage = await readStatisticsRows(page, columns);

  const last = firstPage[firstPage.length - 1];
  const first = secondPage[0];
  if (first.value === last.value) {
    expect(first.position, `first row equals last row of page 1 (${last.value})`).toBe("=");
  } else {
    expect(first.position, `first row differs from last row of page 1 (${last.value})`).toBe(
      String(firstPage.length + 1),
    );
  }
}

/**
 * Checks the page for accessibility violations using axe-core.
 * @param page The page to check.
 * @param disableRules Optional axe rule ids to skip, eg to ignore markup generated by a third-party component.
 * @param exclude Optional CSS selectors for elements to leave out of the scan, eg a third-party iframe whose markup we don't control.
 */
export async function expectNoAccessibilityViolations(
  page: Page,
  disableRules: string[] = [],
  exclude: string[] = [],
) {
  const builder = new AxeBuilder({ page }).disableRules(disableRules);
  for (const selector of exclude) {
    builder.exclude(selector);
  }
  const results = await builder.analyze();

  const violations = results.violations.map(
    ({ id, impact, description, nodes }) =>
      `${id} (${impact}): ${description} [${nodes.length} node${nodes.length === 1 ? "" : "s"}]`,
  );
  expect(
    violations,
    `${violations.length} accessibility violation${violations.length === 1 ? " was" : "s were"} detected`,
  ).toEqual([]);
}
