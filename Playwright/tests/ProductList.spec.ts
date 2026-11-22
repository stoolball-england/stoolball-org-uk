import { expect, test } from "@playwright/test";
import {
  expectValidHtml,
  expectNoAccessibilityViolations,
} from "../support/assertions";

test.describe("Product list", { tag: "@Production" }, () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/shop");
  });

  test("Validates", async ({ page }) => {
    await expectValidHtml(page);
  });

  test("Passes AXE", async ({ page }) => {
    await expectNoAccessibilityViolations(page);
  });

  test("Shows a photo of each product", async ({ page }) => {
    // Product photos are media picker values inside a block list, so this checks they are still read after
    // the legacy media picker was replaced by Media Picker 3
    const products = page.locator(".product-preview");
    await expect(products).not.toHaveCount(0);
    for (const product of await products.all()) {
      await expect(product.locator("img")).toHaveAttribute("src", /^\/media\//);
    }
  });

  test("Has schema.org Product markup for each product", async ({ page }) => {
    const json = await page
      .locator('script[type="application/ld+json"]')
      .textContent();
    const data = JSON.parse(json ?? "");
    expect(data["@context"]).toBe("https://schema.org");

    const products = data["@graph"];
    await expect(page.locator(".product-preview")).toHaveCount(products.length);
    for (const product of products) {
      expect(product["@type"]).toBe("Product");
      expect(product.name).toBeTruthy();
      expect(product.offers["@type"]).toBe("Offer");
      expect(product.offers.priceCurrency).toBe("GBP");
      expect(Number(product.offers.price)).toBeGreaterThan(0);
    }
  });
});
