if (typeof stoolball === "undefined") {
  // deliberately global scope
  var stoolball = {};
}
stoolball.consent = {
  consentVersion: "v1",
  hasFeatureConsent: function () {
    return (
      localStorage.getItem(
        "feature_consent_" + stoolball.consent.consentVersion
      ) === "true"
    );
  },
  hasFeatureDecision: function () {
    return localStorage.getItem(
      "feature_consent_" + stoolball.consent.consentVersion
    );
  },
  setFeatureConsent(hasConsent) {
    localStorage.setItem(
      "feature_consent_" + stoolball.consent.consentVersion,
      hasConsent
    );
  },
  hasMapsConsent: function () {
    return (
      localStorage.getItem(
        "maps_consent_" + stoolball.consent.consentVersion
      ) === "true"
    );
  },
  hasMapsDecision: function () {
    return localStorage.getItem(
      "maps_consent_" + stoolball.consent.consentVersion
    );
  },
  setMapsConsent(hasConsent) {
    localStorage.setItem(
      "maps_consent_" + stoolball.consent.consentVersion,
      hasConsent
    );
  },
  hasTrackingConsent: function () {
    return (
      localStorage.getItem(
        "tracking_consent_" + stoolball.consent.consentVersion
      ) === "true"
    );
  },
  hasTrackingDecision: function () {
    return localStorage.getItem(
      "tracking_consent_" + stoolball.consent.consentVersion
    );
  },
  setTrackingConsent(hasConsent) {
    localStorage.setItem(
      "tracking_consent_" + stoolball.consent.consentVersion,
      hasConsent
    );
  },
  featureListeners: [],
  mapsListeners: [],
  trackingListeners: [],
};

(function () {
  // always try to set up the consent control page - it could be this one
  document.addEventListener("DOMContentLoaded", consentControlPage);

  // if consent recorded, true OR false, return without asking again, unless overridden
  if (
    stoolball.consent.hasFeatureDecision() &&
    stoolball.consent.hasMapsDecision() &&
    stoolball.consent.hasTrackingDecision() &&
    !document.querySelector("[data-show-consent]")
  ) {
    return;
  }

  document.addEventListener("DOMContentLoaded", createConsentBanner);

  function consentControlPage() {
    setupConsentOption(
      "feature_consent",
      stoolball.consent.hasFeatureConsent(),
      stoolball.consent.setFeatureConsent
    );
    setupConsentOption(
      "maps_consent",
      stoolball.consent.hasMapsConsent(),
      stoolball.consent.setMapsConsent
    );
    setupConsentOption(
      "tracking_consent",
      stoolball.consent.hasTrackingConsent(),
      stoolball.consent.setTrackingConsent
    );
  }

  function setupConsentOption(checkboxId, hasConsent, setConsent) {
    const checkbox = document.getElementById(checkboxId);
    if (checkbox) {
      checkbox.checked = hasConsent;
      checkbox.addEventListener("change", function () {
        setConsent(checkbox.checked);
        if (checkbox.checked) {
          runListeners();
        }
      });
    }
  }

  function createConsentBanner() {
    const consent = document.createElement("form");
    consent.classList.add("consent");
    consent.classList.add("d-print-none");
    const container = document.createElement("div");
    container.classList.add("container-xl");
    const request = document.createElement("p");
    request.classList.add("consent__request");
    request.appendChild(
      document.createTextNode(
        "Allow features that save information on your device, like maps & social media?"
      )
    );
    consent.appendChild(container);
    container.appendChild(request);

    const acceptButton = createButton("submit", "btn btn-primary", "Allow all");
    const acceptSelectedButton = createButton(
      "button",
      "btn btn-primary d-none",
      "Allow selected"
    );
    const blockButton = createButton("button", "btn btn-danger", "Block all");
    const choicesButton = createButton(
      "button",
      "btn btn-light",
      "Show choices"
    );

    const choices = document.createElement("div");
    choices.setAttribute("class", "d-none");

    const essential = createCheckbox(
      "essential",
      "Always on",
      "Essential features like signing in"
    );
    essential.querySelector("input").setAttribute("checked", "checked");
    essential.querySelector("input").setAttribute("disabled", "disabled");

    const feature = createCheckbox(
      "feature",
      "Features",
      "Optional features like saving your preferences, with no tracking"
    );
    const maps = createCheckbox(
      "maps",
      "Maps",
      "Show where teams are based and matches are played, but Google Maps will track your visit here"
    );
    const tracking = createCheckbox(
      "tracking",
      "Social media",
      "Better links to services like Facebook and Twitter, but they will track your visit here"
    );

    const linkToCookiePolicy = document.createElement("a");
    linkToCookiePolicy.href = "/privacy/cookies/";
    linkToCookiePolicy.appendChild(document.createTextNode("cookies"));
    const cookiePara = document.createElement("p");
    cookiePara.classList.add("consent__policy");
    cookiePara.appendChild(document.createTextNode("Read more about "));
    cookiePara.appendChild(linkToCookiePolicy);
    cookiePara.appendChild(
      document.createTextNode(" to understand your choices.")
    );

    acceptButton.addEventListener("click", function (e) {
      e.preventDefault();
      stoolball.consent.setFeatureConsent(true);
      stoolball.consent.setMapsConsent(true);
      stoolball.consent.setTrackingConsent(true);
      consent.classList.add("d-none");
      runListeners();
    });

    acceptSelectedButton.addEventListener("click", function (e) {
      e.preventDefault();
      stoolball.consent.setFeatureConsent(
        feature.querySelector("input").checked
      );
      stoolball.consent.setMapsConsent(maps.querySelector("input").checked);
      stoolball.consent.setTrackingConsent(
        tracking.querySelector("input").checked
      );
      consent.classList.add("d-none");
      runListeners();
    });

    choicesButton.addEventListener("click", function (e) {
      e.preventDefault();
      choices.setAttribute("class", "d-block consent__checkboxes");
      acceptSelectedButton.classList.remove("d-none");
      acceptSelectedButton.classList.add("d-inline");
      choicesButton.classList.add("d-none");
      consent.classList.add("consent__choices");
      feature.querySelector("input").focus();
    });

    blockButton.addEventListener("click", function (e) {
      e.preventDefault();
      stoolball.consent.setFeatureConsent(false);
      stoolball.consent.setMapsConsent(false);
      stoolball.consent.setTrackingConsent(false);
      consent.classList.add("d-none");
    });

    choices.appendChild(essential);
    choices.appendChild(feature);
    choices.appendChild(maps);
    choices.appendChild(tracking);
    choices.appendChild(cookiePara);

    container.appendChild(choices);

    const buttons = document.createElement("span");
    buttons.classList.add("consent__buttons");
    buttons.appendChild(acceptButton);
    buttons.appendChild(acceptSelectedButton);
    buttons.appendChild(blockButton);
    buttons.appendChild(choicesButton);
    container.appendChild(buttons);

    const siteHeader = document.querySelector(".header");
    siteHeader.insertBefore(consent, siteHeader.firstChild);
  }

  function createButton(type, className, label) {
    const button = document.createElement("button");
    button.setAttribute("type", type);
    button.setAttribute("class", className);
    button.appendChild(document.createTextNode(label));
    return button;
  }

  function createCheckbox(id, category, description) {
    const container = document.createElement("div");
    container.setAttribute("class", "custom-control custom-checkbox");
    const input = document.createElement("input");
    input.setAttribute("type", "checkbox");
    input.setAttribute("class", "custom-control-input");
    input.setAttribute("id", id);
    const inputLabel = document.createElement("label");
    inputLabel.setAttribute("for", id);
    inputLabel.setAttribute("class", "custom-control-label");
    const categoryTag = document.createElement("strong");
    categoryTag.innerText = category;
    inputLabel.appendChild(categoryTag);
    inputLabel.appendChild(document.createTextNode(" – " + description));
    container.appendChild(input);
    container.appendChild(inputLabel);
    return container;
  }

  function runListeners() {
    if (stoolball.consent.hasFeatureConsent()) {
      stoolball.consent.featureListeners.forEach(function (listener) {
        listener();
      });
    }
    if (stoolball.consent.hasMapsConsent()) {
      stoolball.consent.mapsListeners.forEach(function (listener) {
        listener();
      });
    }
    if (stoolball.consent.hasTrackingConsent()) {
      stoolball.consent.trackingListeners.forEach(function (listener) {
        listener();
      });
    }
  }
})();
