(function () {
  window.addEventListener("DOMContentLoaded", function () {
    /**
     * Prevent double submissions from double-clicks by disabling submit buttons when they're clicked
     * @param {SubmitEvent} e
     * @returns void
     */
    function submitEvent(e) {
      if (e.submitter.tagName !== "BUTTON") {
        return;
      }
      if (!e.submitter.classList.contains("btn-primary")) {
        return;
      }

      e.submitter.classList.add("btn-submitting");
      e.submitter.setAttribute("disabled", "disabled");

      const label = document.createElement("span");
      label.classList.add("btn-submitting__label");
      while (e.submitter.firstChild) {
        label.appendChild(e.submitter.firstChild);
      }
      e.submitter.appendChild(label);

      const spinner = document.createElement("span");
      spinner.classList.add("btn-submitting__spinner");
      e.submitter.appendChild(spinner);
    }

    document.querySelector("main").addEventListener("submit", submitEvent);
  });
})();
