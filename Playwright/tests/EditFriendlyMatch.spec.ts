import { test, expect } from "@playwright/test";

test.describe("Edit friendly match", () => {
  test.describe("When signed out", () => {
    test("Should return 404 for a match in the past", async ({ request }) => {
      const response = await request.get(
        "/matches/maresfield-mixed-education-12jun2014/edit/friendly",
      );
      expect(response.status()).toBe(404);
    });
  });
});
