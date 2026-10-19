import { test, expect } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Leagues and competitions", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/competitions");
  });

  test("Validates", async ({ page }) => {
    await expectValidHtml(page);
  });

  test("Passes AXE", async ({ page }) => {
    await expectNoAccessibilityViolations(page);
  });

  test("Searches when Enter is pressed", async ({ page }) => {
    const results = page.locator(".list-results__title");
    await expect(results).not.toHaveCount(0);
    const totalResultsBeforeSearch = await results.count();

    await page.locator("#competition-search").fill("england");
    await page.locator("#competition-search").press("Enter");

    await expect(page).toHaveURL((url) => url.search === "?q=england");
    await page.waitForLoadState();
    expect(await results.count()).toBeLessThan(totalResultsBeforeSearch);
  });

  test("Searches when the button is clicked", async ({ page }) => {
    const results = page.locator(".list-results__title");
    await expect(results).not.toHaveCount(0);
    const totalResultsBeforeSearch = await results.count();

    await page.locator("#competition-search").fill("england");
    await page.locator(".form-search button").click();

    await expect(page).toHaveURL((url) => url.search === "?q=england");
    await page.waitForLoadState();
    expect(await results.count()).toBeLessThan(totalResultsBeforeSearch);
  });
});
