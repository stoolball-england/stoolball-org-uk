# Requiring consent

Client-side features which require user consent due to their use of cookies or equivalent technology should check for that consent using the `stoolball.consent` namespace and, if it is not already granted, register a listener in case it is granted using the consent banner on the current page load. User consent is stored in local storage, but consumer scripts do not need to know that.

An example consumer script is as follows:

```js
if (typeof stoolball === "undefined") {
  // deliberately global scope
  var stoolball = {};
}
stoolball.exampleFeature = {
  load: function () {
    console.log("load feature");
  },
};
if (stoolball.consent.hasFunctionalConsent()) {
  stoolball.exampleFeature.load();
} else {
  stoolball.consent.functionalListeners.push(stoolball.exampleFeature.load);
}
```

There are three consent levels:

1. **Functional consent**: Optional features that do not track the user.

   Use `stoolball.consent.hasFunctionalConsent()` and the `stoolball.consent.functionalListeners` array.

2. **Improvement consent**: Anonymous tracking for the improvement of this website.

   Use `stoolball.consent.hasImprovementConsent()` and the `stoolball.consent.improvementListeners` array.

3. **Tracking consent**: Personalised tracking used by services like social media and advertising.

   Use `stoolball.consent.hasTrackingConsent()` and the `stoolball.consent.trackingListeners` array.
