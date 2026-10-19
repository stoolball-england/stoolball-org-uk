import { test, expect } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Confirm email address", () => {
  test.describe("Without a key", () => {
    test.beforeEach(async ({ page }) => {
      await page.goto("/account/confirm-email/");
    });

    test("Validates", async ({ page }) => {
      await expectValidHtml(page);
    });

    test("Passes AXE", async ({ page }) => {
      await expectNoAccessibilityViolations(page);
    });

    test("Does not confirm the email address", async ({ page }) => {
      await expect(page.locator("body")).toContainText(
        "Sorry, your request to change your email address could not be confirmed.",
      );
    });
  });
});
