import { expect, test } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Content page", { tag: "@Production" }, () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/rules/what-is-stoolball");
  });

  test("Validates", async ({ page }) => {
    await expectValidHtml(page);
  });

  test("Shows the header photo", async ({ page }) => {
    // The header photo is picked using Media Picker 3, which replaced the legacy media picker
    await expect(page.locator("img.header__photo")).toHaveAttribute(
      "src",
      /^\/media\/.+\?cc=/,
    );
  });

  test("Passes AXE", async ({ page }) => {
    await expectNoAccessibilityViolations(
      page,
      [],
      ["iframe.block__youtube"], // the YouTube player's own markup has ARIA errors we can't fix
    );
  });
});
