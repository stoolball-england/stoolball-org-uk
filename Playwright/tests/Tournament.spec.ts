import { test, expect } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Tournament", { tag: "@Production" }, () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/tournaments/slsa-tournament-23jun2013");
  });

  test("Validates", async ({ page }) => {
    await expectValidHtml(page);
  });

  test("Passes AXE", async ({ page }) => {
    await expectNoAccessibilityViolations(page);
  });

  test("Has schema.org SportsEvent metadata in JSON-LD", async ({ page }) => {
    const json = await page
      .locator('script[type="application/ld+json"]')
      .textContent();
    const tournament = JSON.parse(json ?? "");

    expect(tournament["@context"]).toBe("https://schema.org");
    expect(tournament["@type"]).toBe("SportsEvent");
    expect(tournament.sport).toBe("Stoolball");
    expect(tournament.startDate).toContain("2013-06-23");
    expect(tournament.url).toBe(
      "https://www.stoolball.org.uk/tournaments/slsa-tournament-23jun2013",
    );
  });
});
