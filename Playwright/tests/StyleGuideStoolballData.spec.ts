import { test } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Style guide - stoolball data section", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/style-guide/?alttemplate=styleguidestoolballdata");
  });

  test("Validates", async ({ page }) => {
    await expectValidHtml(page, {
      rules: {
        "input-missing-label": "off", // not smart enough to realise fields are labelled by table headers
      },
    });
  });

  test("Passes AXE", async ({ page }) => {
    await expectNoAccessibilityViolations(page);
  });
});
