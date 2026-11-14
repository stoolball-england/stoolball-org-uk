import { test, expect } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Match location", { tag: "@Production" }, () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/locations/maresfield-recreation-ground");
  });

  test("Validates", async ({ page }) => {
    await expectValidHtml(page);
  });

  test("Passes AXE", async ({ page }) => {
    await expectNoAccessibilityViolations(page);
  });

  test("Has schema.org Place metadata in JSON-LD", async ({ page }) => {
    const json = await page
      .locator('script[type="application/ld+json"]')
      .textContent();
    const place = JSON.parse(json ?? "");

    expect(place["@context"]).toBe("https://schema.org");
    expect(place["@type"]).toBe("Place");
    expect(place.name).toContain("Maresfield");
    expect(place.url).toBe(
      "https://www.stoolball.org.uk/locations/maresfield-recreation-ground",
    );
    expect(place.address["@type"]).toBe("PostalAddress");
  });
});
