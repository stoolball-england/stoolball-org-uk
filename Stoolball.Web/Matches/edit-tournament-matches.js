(function () {
  window.addEventListener("DOMContentLoaded", function () {
    // Validate the select elements used to add a match.
    // Update attributes and error messages.
    function validateMatch(row, select) {
      const fields = row.querySelectorAll("select");
      const validator = row.querySelector(".add-match-validation");
      if (
        fields[0] === select &&
        select.selectedIndex > 0 &&
        select.selectedIndex !== fields[1].selectedIndex
      ) {
        fields[0].setAttribute("aria-invalid", "false");
        if (fields[1].selectedIndex > 0) {
          fields[1].setAttribute("aria-invalid", "false");
          validator.innerHTML = "";
        }
      } else if (
        fields[1] === select &&
        select.selectedIndex > 0 &&
        select.selectedIndex !== fields[0].selectedIndex
      ) {
        fields[1].setAttribute("aria-invalid", "false");
        if (fields[0].selectedIndex > 0) {
          validator.innerHTML = "";
        }
      } else if (
        fields[0].selectedIndex > 0 &&
        fields[0].selectedIndex === fields[1].selectedIndex
      ) {
        fields[0].setAttribute("aria-invalid", "false");
        fields[1].setAttribute("aria-invalid", "true");
        validator.innerHTML = validator.getAttribute("data-val-msg-diff");
      } else {
        if (fields[0] === select && fields[0].selectedIndex === 0) {
          fields[0].setAttribute("aria-invalid", "true");
          validator.innerHTML = validator.getAttribute("data-val-msg");
        }
        if (fields[1] == select && fields[1].selectedIndex === 0) {
          fields[1].setAttribute("aria-invalid", "true");
          validator.innerHTML = validator.getAttribute("data-val-msg");
        }
      }
      if (validator.innerHTML) {
        validator.classList.remove("field-validation-valid");
        validator.classList.add("field-validation-error");
      } else {
        validator.classList.add("field-validation-valid");
        validator.classList.remove("field-validation-error");
      }
    }

    function onBlur() {
      let row = this;
      while (row.tagName !== "TR") {
        row = row.parentNode;
      }
      const fields = row.querySelectorAll("select");
      let matchName =
        (fields[0].options[fields[0].selectedIndex].text ||
          "No team selected") +
        " v " +
        (fields[1].options[fields[1].selectedIndex].text || "No team selected");

      row.querySelector(".select-teams-in-match__match-name").value = matchName;

      row
        .querySelector("img")
        .setAttribute("alt", "Remove '" + matchName + "' from the tournament");

      validateMatch(row, this);
    }

    // When 'Add match' is clicked add a row to the table and update the indexes.
    const addButton = document.querySelector(".select-teams-in-match__add");
    const thisEditor = document.querySelector(".related-items");
    addButton.addEventListener("click", function () {
      const template = document.getElementById(
        this.getAttribute("data-template")
      ).innerHTML;

      const tbody = thisEditor.querySelector("tbody");
      tbody.insertAdjacentHTML(
        "beforeend",
        thisEditor.relatedItems.populateTemplate(template, {
          value: "",
          data: {},
        })
      );

      thisEditor.relatedItems.resetEmpty(thisEditor);
      thisEditor.relatedItems.resetIndexes(thisEditor.lastChild);

      let rows = thisEditor.querySelectorAll("tr");
      const fields = rows[rows.length - 1].querySelectorAll(
        ".select-teams-in-match select"
      );

      // Watch for the blur event on the select elements and update validation.
      fields[0].addEventListener("blur", onBlur);
      fields[1].addEventListener("blur", onBlur);

      // alert assistive technology
      thisEditor.relatedItems.alertAssistiveTechnology(
        document,
        thisEditor,
        "new match"
      );

      // set focus back to the first dropdown
      fields[fields.length - 2].focus();
    });

    // If we've hit server-side validation for some reason, there could be select lists already in the table, so wire up the blur event
    var selects = thisEditor.querySelectorAll("select");
    for (let i = 0; i < selects.length; i++) {
      selects[i].addEventListener("blur", onBlur);
    }

    // When 'Save' is clicked, make sure there are no errors
    const form = document.querySelector("main form");
    form.addEventListener("submit", function (e) {
      if (this.querySelector("[aria-invalid='true']")) {
        e.preventDefault();
        return false;
      }
      return true;
    });
  });
})();
