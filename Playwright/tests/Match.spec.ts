import { test, expect } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Match", { tag: "@Production" }, () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/matches/maresfield-mixed-new-school-ninjas-22jul2021");
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
    const match = JSON.parse(json ?? "");

    expect(match["@context"]).toBe("https://schema.org");
    expect(match["@type"]).toBe("SportsEvent");
    expect(match.name).toContain("Maresfield");
    expect(match.sport).toBe("Stoolball");
    expect(match.startDate).toContain("2021-07-22");
    expect(match.url).toBe(
      "https://www.stoolball.org.uk/matches/maresfield-mixed-new-school-ninjas-22jul2021",
    );
  });
});
