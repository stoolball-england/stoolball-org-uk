import { test, expect } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Edit club", () => {
  test.describe("When signed out", () => {
    test.beforeEach(async ({ page }) => {
      await page.goto("/clubs/maresfield/edit/club");
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
