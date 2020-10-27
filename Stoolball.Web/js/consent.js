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
  // if consent recorded, true OR false, return without asking again
  if (
    localStorage.getItem("functional_consent") &&
    localStorage.getItem("improvement_consent") &&
    localStorage.getItem("tracking_consent")
  ) {
    return;
  }

  document.addEventListener("DOMContentLoaded", function () {
    const optIn = document.createElement("form");
    optIn.setAttribute("class", "opt-in container-xl");
    optIn.appendChild(
      document.createTextNode(
        "Some features of this site store information on your computer. Is that OK?"
      )
    );

    const acceptButton = createButton(
      "btn btn-primary",
      "Accept all and close"
    );
    const acceptSelectedButton = createButton(
      "btn btn-primary d-none",
      "Accept selected"
    );
    const rejectButton = createButton("btn btn-secondary", "Reject all");
    const choicesButton = createButton("btn btn-secondary", "Show choices");

    const choices = document.createElement("div");
    choices.setAttribute("class", "d-none");

    const essential = createCheckbox(
      "essential",
      "Always on (essential features like signing in)"
    );
    essential.querySelector("input").setAttribute("checked", "checked");
    essential.querySelector("input").setAttribute("disabled", "disabled");

    const functional = createCheckbox(
      "functional",
      "Optional features (make this site work better, with no tracking)"
    );
    const improvement = createCheckbox(
      "improvement",
      "Help to improve this site with anonymous statistics (Google Analytics)"
    );
    const tracking = createCheckbox(
      "tracking",
      "Social media (show better links to services like Facebook and Twitter, but they will track you)"
    );

    acceptButton.addEventListener("click", function (e) {
      e.preventDefault();
      saveChoices(true, true, true);
      optIn.classList.add("d-none");
      runListeners();
    });

    acceptSelectedButton.addEventListener("click", function (e) {
      e.preventDefault();
      saveChoices(
        functional.querySelector("input").checked,
        improvement.querySelector("input").checked,
        tracking.querySelector("input").checked
      );
      optIn.classList.add("d-none");
      runListeners();
    });

    choicesButton.addEventListener("click", function (e) {
      e.preventDefault();
      choices.setAttribute("class", "d-block");
      acceptSelectedButton.classList.remove("d-none");
      acceptSelectedButton.classList.add("d-inline");
      choicesButton.classList.add("d-none");
    });

    rejectButton.addEventListener("click", function (e) {
      e.preventDefault();
      saveChoices(false, false, false);
      optIn.classList.add("d-none");
    });

    choices.appendChild(essential);
    choices.appendChild(functional);
    choices.appendChild(improvement);
    choices.appendChild(tracking);

    optIn.appendChild(choices);

    optIn.appendChild(acceptButton);
    optIn.appendChild(acceptSelectedButton);
    optIn.appendChild(rejectButton);
    optIn.appendChild(choicesButton);

    document.body.insertBefore(optIn, document.body.firstChild);
  });

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
