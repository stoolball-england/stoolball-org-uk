"use strict";

// For Jest tests
if (typeof module !== "undefined" && typeof module.exports !== "undefined") {
  module.exports = createFilterUI;
}

function createFilterUI() {
  return {};
}

(function () {
  window.addEventListener("DOMContentLoaded", function () {
    const toggle = document.querySelector(".nav-link-filter");
    const edit = document.querySelector(".filter__edit");
    const applied = document.querySelector(".filter__applied");
    if (!toggle || !edit) {
      return;
    }

    const filterUI = createFilterUI();

    toggle.addEventListener("click", function () {
      if (edit.classList.contains("d-none")) {
        edit.classList.remove("d-none");
        if (applied) {
          applied.classList.add("d-none");
        }
      }
      edit.querySelector("input").focus();
    });
  });
})();
