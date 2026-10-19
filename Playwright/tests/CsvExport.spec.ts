import { test, expect } from "@playwright/test";

test.describe("CSV export", () => {
  test("Should return onlysport.csv without a key", async ({ request }) => {
    const response = await request.get("/data/onlysport.csv");
    expect(response.ok()).toBe(true);
  });

  test("Should return 404 for spogo.csv without a key", async ({ request }) => {
    const response = await request.get("/data/spogo.csv");
    expect(response.status()).toBe(404);
  });

  test("Should return 404 for spogo.csv without an invalid key", async ({
    request,
  }) => {
    const response = await request.get("/data/spogo.csv?key=invalid");
    expect(response.status()).toBe(404);
  });

  test("Should return spogo.csv with a valid key", async ({ request }) => {
    const apiKey = process.env.CSV_EXPORT_API_KEY;
    expect(
      apiKey,
      "CSV_EXPORT_API_KEY must be set. See docs/Testing.md.",
    ).toBeTruthy();

    const response = await request.get(`/data/spogo.csv?key=${apiKey}`);
    expect(response.ok()).toBe(true);
  });
});
