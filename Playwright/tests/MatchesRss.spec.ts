import { test, expect } from "@playwright/test";

test.describe("Matches RSS feed", () => {
  test.describe("In standard format", () => {
    test("Loads", async ({ request }) => {
      const response = await request.get("/matches.rss");
      expect(response.ok()).toBe(true);
    });
  });
  test.describe("In Twitter format", () => {
    test("Loads", async ({ request }) => {
      const response = await request.get("/matches.rss?format=tweet");
      expect(response.ok()).toBe(true);
    });
  });
});
