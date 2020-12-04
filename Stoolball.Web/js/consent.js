if (typeof stoolball === "undefined") {
  // deliberately global scope
  var stoolball = {};
}
stoolball.consent = {
  hasFunctionalConsent: function () {
    return localStorage.getItem("functional_consent") === "true";
  },
  hasImprovementConsent: function () {
    return localStorage.getItem("improvement_consent") === "true";
  },
  hasTrackingConsent: function () {
    return localStorage.getItem("tracking_consent") === "true";
  },
  functionalListeners: [],
  improvementListeners: [],
  trackingListeners: [],
};

(function () {
  // always try to set up the consent control page - it could be this one
  document.addEventListener("DOMContentLoaded", consentControlPage);

  // if consent recorded, true OR false, return without asking again, unless overridden
  if (
    localStorage.getItem("functional_consent") &&
    localStorage.getItem("improvement_consent") &&
    localStorage.getItem("tracking_consent") &&
    !document.querySelector("[data-show-consent]")
  ) {
    return;
  }

  document.addEventListener("DOMContentLoaded", createConsentBanner);

  function consentControlPage() {
    setupConsentOption("functional");
    setupConsentOption("improvement");
    setupConsentOption("tracking");
  }

  function setupConsentOption(id) {
    id = id + "_consent";
    const checkbox = document.getElementById(id);
    if (checkbox) {
      checkbox.checked = localStorage.getItem(id) === "true";
      checkbox.addEventListener("change", function () {
        localStorage.setItem(id, checkbox.checked);
        if (checkbox.checked) {
          runListeners();
        }
      });
    }
  }

  function createConsentBanner() {
    const consent = document.createElement("form");
    consent.classList.add("consent");
    const container = document.createElement("div");
    container.classList.add("container-xl");
    const request = document.createElement("p");
    request.classList.add("consent__request");
    request.appendChild(
      document.createTextNode(
        "Our website works better if it can save information on your device. Is that OK?"
      )
    );
    consent.appendChild(container);
    container.appendChild(request);

    const acceptButton = createButton(
      "btn btn-primary",
      "Accept all and close"
    );
    const acceptSelectedButton = createButton(
      "btn btn-primary d-none",
      "Accept selected"
    );
    const rejectButton = createButton("btn btn-danger", "Reject all");
    const choicesButton = createButton("btn btn-light", "Show choices");

    const choices = document.createElement("div");
    choices.setAttribute("class", "d-none");

    const essential = createCheckbox(
      "essential",
      "Always on – essential features like signing in"
    );
    essential.querySelector("input").setAttribute("checked", "checked");
    essential.querySelector("input").setAttribute("disabled", "disabled");

    const functional = createCheckbox(
      "functional",
      "Optional features – make this site work better, with no tracking"
    );
    const improvement = createCheckbox(
      "improvement",
      "Help to improve this site with anonymous statistics"
    );
    const tracking = createCheckbox(
      "tracking",
      "Social media – show better links to services like Facebook and Twitter, but they will track you"
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
      saveChoices(true, true, true);
      consent.classList.add("d-none");
      runListeners();
    });

    acceptSelectedButton.addEventListener("click", function (e) {
      e.preventDefault();
      saveChoices(
        functional.querySelector("input").checked,
        improvement.querySelector("input").checked,
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
      functional.querySelector("input").focus();
    });

    rejectButton.addEventListener("click", function (e) {
      e.preventDefault();
      saveChoices(false, false, false);
      consent.classList.add("d-none");
    });

    choices.appendChild(essential);
    choices.appendChild(functional);
    choices.appendChild(improvement);
    choices.appendChild(tracking);
    choices.appendChild(cookiePara);

    container.appendChild(choices);

    const buttons = document.createElement("span");
    buttons.classList.add("consent__buttons");
    buttons.appendChild(acceptButton);
    buttons.appendChild(acceptSelectedButton);
    buttons.appendChild(rejectButton);
    buttons.appendChild(choicesButton);
    container.appendChild(buttons);

    document.body.insertBefore(consent, document.body.firstChild);
  }

  function createButton(className, label) {
    const button = document.createElement("button");
    button.setAttribute("class", className);
    button.appendChild(document.createTextNode(label));
    return button;
  }

  function createCheckbox(id, label) {
    const container = document.createElement("div");
    container.setAttribute("class", "custom-control custom-checkbox");
    const input = document.createElement("input");
    input.setAttribute("type", "checkbox");
    input.setAttribute("class", "custom-control-input");
    input.setAttribute("id", id);
    const inputLabel = document.createElement("label");
    inputLabel.setAttribute("for", id);
    inputLabel.setAttribute("class", "custom-control-label");
    inputLabel.appendChild(document.createTextNode(label));
    container.appendChild(input);
    container.appendChild(inputLabel);
    return container;
  }

  function saveChoices(functional, improvement, tracking) {
    localStorage.setItem("functional_consent", functional);
    localStorage.setItem("improvement_consent", improvement);
    localStorage.setItem("tracking_consent", tracking);
  }

  function runListeners() {
    if (localStorage.getItem("functional_consent") === "true") {
      stoolball.consent.functionalListeners.forEach(function (listener) {
        listener();
      });
    }
    if (localStorage.getItem("improvement_consent") === "true") {
      stoolball.consent.improvementListeners.forEach(function (listener) {
        listener();
      });
    }
    if (localStorage.getItem("tracking_consent") === "true") {
      stoolball.consent.trackingListeners.forEach(function (listener) {
        listener();
      });
    }
  }
})();
