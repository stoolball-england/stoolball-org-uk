if (typeof stoolball === "undefined") {
  // deliberately global scope
  var stoolball = {};
}
stoolball.analytics = {
  setup: function () {
    if (typeof window.dataLayer !== "undefined") {
      return;
    }
    const gtm = document.createElement("script");
    gtm.setAttribute("async", "async");
    gtm.setAttribute(
      "src",
      "https://www.googletagmanager.com/gtag/js?id=G-4HCNJHS2HV"
    );
    document.head.appendChild(gtm);

    window.dataLayer = window.dataLayer || [];
    function gtag() {
      dataLayer.push(arguments);
    }
    gtag("js", new Date());
    gtag("config", document.documentElement.getAttribute("data-analytics"), {
      allow_google_signals: false,
      allow_ad_personalization_signals: false,
    });
  },
};
if (stoolball.consent.hasAnalyticsConsent()) {
  stoolball.analytics.setup();
} else {
  stoolball.consent.analyticsListeners.push(stoolball.analytics.setup);
}
