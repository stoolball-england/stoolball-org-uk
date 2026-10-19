import { defineConfig, devices } from "@playwright/test";
import { existsSync } from "fs";
import { join } from "path";

// Secrets used by tests, such as CSV_EXPORT_API_KEY, can be set in Playwright/.env, which is ignored by git.
const envFile = join(__dirname, ".env");
if (existsSync(envFile)) {
  process.loadEnvFile(envFile);
}

export default defineConfig({
  testDir: "./tests",
  outputDir: "./test-results",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: [
    ["list"],
    ["html", { outputFolder: "./playwright-report", open: "never" }],
  ],
  use: {
    baseURL: process.env.BASE_URL ?? "https://localhost:44343",
    ignoreHTTPSErrors: true,
    trace: "on-first-retry",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
