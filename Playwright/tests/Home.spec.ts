import { test, expect } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Home page", { tag: "@Production" }, () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/");
  });

  test("Validates", async ({ page }) => {
    await expectValidHtml(page);
  });

  test("Passes AXE", async ({ page }) => {
    await expectNoAccessibilityViolations(page);
  });

  test("Has schema.org NGO metadata in JSON-LD", async ({ page }) => {
    const json = await page
      .locator('script[type="application/ld+json"]')
      .textContent();
    const organisation = JSON.parse(json ?? "");

    expect(organisation["@context"]).toBe("https://schema.org");
    expect(organisation["@type"]).toBe("NGO");
    expect(organisation.name).toBe("Stoolball England");
    expect(organisation.url).toBe("https://www.stoolball.org.uk/");
    expect(organisation.logo).toBe(
      "https://www.stoolball.org.uk/images/logos/stoolball-england.svg",
    );
    expect(organisation.sameAs).toContain("https://facebook.com/stoolball");
    expect(organisation.sameAs).toContain(
      "https://youtube.com/stoolballengland",
    );
    expect(organisation.address).toBeUndefined();
  });

  test("Links to 'what is stoolball?'", async ({ page }) => {
    await page.getByText("Read more about stoolball").first().click();
    await expect(page).toHaveURL(/\/rules\/what-is-stoolball/);
  });
});
