import { test, expect } from "@playwright/test";

test.describe("Sitewide headers", () => {
  test("Include security headers", async ({ request }) => {
    const response = await request.get("/");
    const headers = response.headers();

    expect(headers).not.toHaveProperty("server");
    expect(headers).not.toHaveProperty("x-powered-by");
    expect(headers["permissions-policy"]).toBe(
      "accelerometer=(),ambient-light-sensor=(),autoplay=(),battery=(),camera=(),cross-origin-isolated=(),display-capture=(),document-domain=(),encrypted-media=(),execution-while-not-rendered=(),execution-while-out-of-viewport=(),fullscreen=(),geolocation=(),gyroscope=(),magnetometer=(),microphone=(),midi=(),navigation-override=(),payment=(),picture-in-picture=(),publickey-credentials-get=(),screen-wake-lock=(),sync-xhr=(),usb=(),web-share=(),xr-spatial-tracking=()",
    );
    expect(headers["referrer-policy"]).toBe("no-referrer-when-downgrade");
    expect(headers["strict-transport-security"]).toBe("max-age=31536000");
    expect(headers["x-content-type-options"]).toBe("nosniff");
    expect(headers["x-frame-options"]).toBe("SAMEORIGIN");
    expect(headers["x-xss-protection"]).toBe("1; mode=block");
    expect(headers["report-to"]).toBe(
      '{"group":"default","max_age":31536000,"endpoints":[{"url":"https://stoolball.report-uri.com/a/d/g"}],"include_subdomains":true}',
    );
    expect(headers["nel"]).toBe(
      '{"report_to":"default","max_age":31536000,"include_subdomains":true}',
    );
    expect(headers["cross-origin-opener-policy"]).toBe(
      'same-origin; report-to="default"',
    );
  });

  test("Remove headers that break the back office", async ({ request }) => {
    const response = await request.get("/umbraco");
    const headers = response.headers();

    expect(headers).not.toHaveProperty("permissions-policy");
    expect(headers).not.toHaveProperty("cross-origin-opener-policy");
  });

  test("Have CORP headers on static files", async ({ request }) => {
    const response = await request.get("/");
    expect(response.headers()["cross-origin-resource-policy"]).not.toBe(
      "same-origin",
    );

    const paths = [
      "/images/logos/stoolball-england.svg",
      "/images/logos/stoolball-england-512.png",
      "/images/logos/stoolball-england-rss.gif",
      "/images/photos/baseball.jpg",
      "/favicon.ico",
      "/js/related-item.js",
      "/css/base.min.css",
    ];

    for (const path of paths) {
      const staticResponse = await request.get(path);
      expect(staticResponse.ok(), path).toBe(true);
      expect(
        staticResponse.headers()["cross-origin-resource-policy"],
        path,
      ).toBe("same-origin");
    }
  });
});
