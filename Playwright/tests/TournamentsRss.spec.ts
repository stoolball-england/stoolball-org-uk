import { test, expect } from "@playwright/test";

test.describe("Tournaments RSS feed", () => {
  test.describe("In standard format", () => {
    test("Loads", async ({ request }) => {
      const response = await request.get("/tournaments.rss");
      expect(response.ok()).toBe(true);
    });
  });
  test.describe("In Twitter format", () => {
    test("Loads", async ({ request }) => {
      const response = await request.get("/tournaments.rss?format=tweet");
      expect(response.ok()).toBe(true);
    });
  });
});
