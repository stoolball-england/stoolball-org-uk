import { test, expect } from "@playwright/test";

test.describe("Matches calendar", () => {
  test("Loads", async ({ request }) => {
    const response = await request.get("/matches.ics");
    expect(response.ok()).toBe(true);
  });
});
