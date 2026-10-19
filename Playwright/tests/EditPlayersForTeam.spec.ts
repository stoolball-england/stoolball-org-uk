import { test, expect } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Edit players for team", () => {
  test.describe("When signed out", () => {
    test.beforeEach(async ({ page }) => {
      await page.goto("/teams/maresfield-mixed/edit/players");
    });

    test("Validates", async ({ page }) => {
      await expectValidHtml(page);
    });

    test("Passes AXE", async ({ page }) => {
      await expectNoAccessibilityViolations(page);
    });

    test("Requires authentication", async ({ page }) => {
      await expect(page.locator("body")).toContainText("Sign in");
    });
  });
});
