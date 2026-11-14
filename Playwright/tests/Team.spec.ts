import { test, expect } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Team", { tag: "@Production" }, () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/teams/maresfield-mixed");
  });

  test("Validates", async ({ page }) => {
    await expectValidHtml(page);
  });

  test("Passes AXE", async ({ page }) => {
    await expectNoAccessibilityViolations(page);
  });

  test("Has schema.org SportsTeam metadata in JSON-LD", async ({ page }) => {
    const json = await page
      .locator('script[type="application/ld+json"]')
      .textContent();
    const team = JSON.parse(json ?? "");

    expect(team["@context"]).toBe("https://schema.org");
    expect(team["@type"]).toBe("SportsTeam");
    expect(team.name).toContain("Maresfield");
    expect(team.sport).toBe("Stoolball");
    expect(team.url).toBe("https://www.stoolball.org.uk/teams/maresfield-mixed");
  });
});
