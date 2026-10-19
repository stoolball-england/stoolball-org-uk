import { test } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Transient team", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto(
      "/tournaments/lewes-arms-tournament-7jul2013/teams/brighton-beachcombers/edit",
    );
  });

  test("Passes AXE", async ({ page }) => {
    await expectNoAccessibilityViolations(page);
  });

  test("Validates", async ({ page }) => {
    await expectValidHtml(page);
  });
});
