import { test, expect } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Request password reset", { tag: "@Production" }, () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/account/reset-password/");
  });

  test("Validates", async ({ page }) => {
    await expectValidHtml(page);
  });

  test("Passes AXE", async ({ page }) => {
    await expectNoAccessibilityViolations(page);
  });

  test("Validation messages use sentence case", async ({ page }) => {
    // The field name goes in the middle of the message, so is not capitalised
    await expect(page.locator("input[type=email]")).toHaveAttribute(
      "data-val-required",
      "The email field is required.",
    );
  });
});
