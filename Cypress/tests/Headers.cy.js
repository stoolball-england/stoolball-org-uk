describe("Sitewide headers", () => {
  it("Include security headers", () => {
    cy.request("/").then((response) => {
      expect(response.headers).not.to.have.property("server");
      expect(response.headers).not.to.have.property("x-powered-by");
      expect(response.headers).to.have.property(
        "permissions-policy",
        "accelerometer=(),ambient-light-sensor=(),autoplay=(),battery=(),camera=(),cross-origin-isolated=(),display-capture=(),document-domain=(),encrypted-media=(),execution-while-not-rendered=(),execution-while-out-of-viewport=(),fullscreen=(),geolocation=(),gyroscope=(),magnetometer=(),microphone=(),midi=(),navigation-override=(),payment=(),picture-in-picture=(),publickey-credentials-get=(),screen-wake-lock=(),sync-xhr=(),usb=(),web-share=(),xr-spatial-tracking=()"
      );
      expect(response.headers).to.have.property(
        "referrer-policy",
        "no-referrer-when-downgrade"
      );
      expect(response.headers).to.have.property(
        "strict-transport-security",
        "max-age=31536000"
      );
      expect(response.headers).to.have.property(
        "x-content-type-options",
        "nosniff"
      );
      expect(response.headers).to.have.property(
        "x-frame-options",
        "SAMEORIGIN"
      );
      expect(response.headers).to.have.property(
        "x-xss-protection",
        "1; mode=block"
      );
      expect(response.headers).to.have.property(
        "report-to",
        '{"group":"default","max_age":31536000,"endpoints":[{"url":"https://stoolball.report-uri.com/a/d/g"}],"include_subdomains":true}'
      );
      expect(response.headers).to.have.property(
        "nel",
        '{"report_to":"default","max_age":31536000,"include_subdomains":true}'
      );
      expect(response.headers).to.have.property(
        "cross-origin-opener-policy",
        'same-origin; report-to="default"'
      );
    });
  });

  it("Remove headers that break the back office", () => {
    cy.request({ url: "/umbraco", failOnStatusCode: false }).then(
      (response) => {
        expect(response.headers).not.to.have.property("permissions-policy");
        expect(response.headers).not.to.have.property(
          "cross-origin-opener-policy"
        );
      }
    );
  });

  it("Have CORP headers on static files", () => {
    cy.request("/").then((response) => {
      expect(response.headers).not.to.have.property(
        "cross-origin-resource-policy",
        "same-origin"
      );
    });

    const paths = [
      "/images/logos/stoolball-england.svg",
      "/images/logos/stoolball-england-512.png",
      "/images/logos/stoolball-england-rss.gif",
      "/images/photos/baseball.jpg",
      "/favicon.ico",
      "/js/related-item.js",
      "/css/base.min.css",
    ];

    for (let i = 0; i < paths.length; i++) {
      cy.request(paths[i]).then((response) => {
        expect(response.headers).to.have.property(
          "cross-origin-resource-policy",
          "same-origin"
        );
      });
    }
  });
});
