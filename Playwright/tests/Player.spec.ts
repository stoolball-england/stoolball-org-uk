import { test, expect } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Player summary", { tag: "@Production" }, () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/players/rick-mason");
  });

  test("Validates", async ({ page }) => {
    await expectValidHtml(page);
  });

  test("Passes AXE", async ({ page }) => {
    await expectNoAccessibilityViolations(page);
  });

  // Relies on this player being added to a member's statistics, both locally and in production
  test("Does not link to add a player already added to a member's statistics", async ({
    page,
  }) => {
    await expect(page.locator('a[href$="/add-to-my-statistics"]')).toHaveCount(
      0,
    );
  });
});

test.describe("Player summary for a player not added to a member's statistics", { tag: "@Production" }, () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/players/abbie-newell");
  });

  // Relies on this player not being added to a member's statistics, both locally and in production
  test("Links to add the player to my statistics", async ({ page }) => {
    await expect(
      page.getByRole("link", { name: "Add Abbie Newell to my statistics" }),
    ).toHaveAttribute("href", "/players/abbie-newell/add-to-my-statistics");
  });
});
