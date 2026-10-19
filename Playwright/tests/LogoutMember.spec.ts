import { test } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Logout member", () => {
  test.describe("When signed out", () => {
    test.beforeEach(async ({ page }) => {
      await page.goto("/account/sign-out/");
    });

    test("Validates", async ({ page }) => {
      await expectValidHtml(page);
    });
  });

  test("Passes AXE", async ({ page }) => {
    await expectNoAccessibilityViolations(page);
  });
});
