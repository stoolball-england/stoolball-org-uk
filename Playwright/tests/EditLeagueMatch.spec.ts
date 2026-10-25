import { test, expect } from "@playwright/test";

test.describe("Edit league match", { tag: "@Production" }, () => {
  test.describe("When signed out", () => {
    test("Should return 404 for a match in the past", async ({ request }) => {
      const response = await request.get(
        "/matches/maresfield-mixed-east-hoathly-and-halland-27may2014/edit/league",
      );
      expect(response.status()).toBe(404);
    });
  });
});
