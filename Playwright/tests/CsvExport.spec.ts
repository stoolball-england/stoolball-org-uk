import { test, expect } from "@playwright/test";

test.describe("CSV export", { tag: "@Production" }, () => {
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

  test("Should return spogo.csv with a valid key", async ({
    request,
    baseURL,
  }) => {
    // The production key is kept separately so one Playwright/.env file works for both local and production runs
    const keyName =
      new URL(baseURL!).hostname === "localhost"
        ? "CSV_EXPORT_API_KEY"
        : "PRODUCTION_CSV_EXPORT_API_KEY";
    const apiKey = process.env[keyName];
    expect(apiKey, `${keyName} must be set. See docs/Testing.md.`).toBeTruthy();

    const response = await request.get(`/data/spogo.csv?key=${apiKey}`);
    expect(response.ok()).toBe(true);
  });
});
