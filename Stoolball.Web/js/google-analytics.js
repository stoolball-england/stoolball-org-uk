if (typeof stoolball === "undefined") {
  // deliberately global scope
  var stoolball = {};
}
stoolball.analytics = {
  load: function () {
    console.log("load analytics");
  },
};
if (stoolball.consent.hasImprovementConsent()) {
  stoolball.analytics.load();
} else {
  stoolball.consent.improvementListeners.push(stoolball.analytics.load);
}
