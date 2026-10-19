import { test, expect } from "@playwright/test";

test.describe("Edit knockout match", () => {
  test.describe("When signed out", () => {
    test("Should return 404 for a match in the past", async ({ request }) => {
      const response = await request.get(
        "/matches/east-hoathly-and-halland-nutley-mixed-3jun2014/edit/knockout",
      );
      expect(response.status()).toBe(404);
    });
  });
});
