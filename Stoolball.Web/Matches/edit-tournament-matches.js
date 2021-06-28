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
        .querySelector(".btn-delete-icon img")
        .setAttribute("alt", "Remove '" + matchName + "' from the tournament");

      row
        .querySelector(".btn-drag img")
        .setAttribute("alt", "Move '" + matchName + "' up or down");

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

      // Watch for click event on drag handle
      rows[rows.length - 1]
        .querySelector(".select-teams-in-match__sort")
        .addEventListener("click", onDrag);

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

    // Pointer-based drag-and-drop
    if (typeof Sortable !== "undefined") {
      Sortable.create(
        document.querySelector(".select-teams-in-match__matches"),
        {
          handle: ".select-teams-in-match__sort",
          animation: 150,
          onEnd: function () {
            thisEditor.relatedItems.resetIndexes(thisEditor.lastChild);
          },
        }
      );
    }

    // Let go of any items grabbed for drag & drop and release any drag handle
    // toggle button that grabbed the item.
    function resetGrabbed(except) {
      const grabbed = thisEditor.querySelectorAll("[aria-grabbed='true']");
      for (let i = 0; i < grabbed.length; i++) {
        if (except && grabbed[i] === except) {
          continue;
        }
        grabbed[i].setAttribute("aria-grabbed", "false");
        grabbed[i]
          .querySelector("[aria-pressed='true']")
          .setAttribute("aria-pressed", "false");
      }
    }

    // Changing the role allows arrow keys to be intercepted and handled,
    // but it should only be set when a drag handle button is pressed.
    function setApplicationRole(anyChildElement, isApplication) {
      let dropTarget = anyChildElement;
      while (dropTarget.tagName !== "TBODY") {
        dropTarget = dropTarget.parentNode;
      }
      if (isApplication) {
        dropTarget.setAttribute("role", "application");
      } else {
        dropTarget.removeAttribute("role");
      }
    }

    // Keyboard-based drag-and-drop
    function onDrag(e) {
      // Find the row to grab
      let dragTarget = e.target;
      while (dragTarget.tagName !== "TR") {
        dragTarget = dragTarget.parentNode;
      }

      // Reset any existing grabbed items
      resetGrabbed(dragTarget);

      // Toggle the drag handle button and grab the row it represents
      const pressed = e.target.getAttribute("aria-pressed") === "false";
      e.target.setAttribute("aria-pressed", pressed);
      dragTarget.setAttribute("aria-grabbed", pressed);

      // Change the role to application so we can handle key presses
      setApplicationRole(e.target, pressed);
    }

    let draggable = thisEditor.querySelectorAll(".select-teams-in-match__sort");
    for (let i = 0; i < draggable.length; i++) {
      draggable[i].addEventListener("click", onDrag);
    }

    function setStatus(text) {
      const status = document.querySelector("[role='status']");
      status.innerHTML = text;
      setTimeout(function () {
        status.innerHTML = "";
      }, 4000);
    }

    function getElementIndex(node) {
      let index = 0;
      while ((node = node.previousElementSibling)) {
        index++;
      }
      return index;
    }

    // Handle key presses for keyboard drag and drop
    thisEditor.addEventListener("keydown", function (e) {
      const grabbed = thisEditor.querySelector("[aria-grabbed='true']");
      if (grabbed) {
        const tab = 9,
          esc = 27,
          upArrow = 38,
          downArrow = 40;
        if (e.keyCode === tab || e.keyCode == esc) {
          resetGrabbed(null);
          setApplicationRole(grabbed, false);
        } else if (e.keyCode === upArrow) {
          if (grabbed !== grabbed.parentElement.firstElementChild) {
            const previousSibling = grabbed.previousElementSibling;
            grabbed.parentElement.removeChild(grabbed);
            previousSibling.parentElement.insertBefore(
              grabbed,
              previousSibling
            );
            grabbed.querySelector("[aria-pressed='true']").focus();
            thisEditor.relatedItems.resetIndexes(thisEditor.lastChild);
            setStatus("Moved up to position " + (getElementIndex(grabbed) + 1));
          }
        } else if (e.keyCode === downArrow) {
          if (grabbed !== grabbed.parentElement.lastElementChild) {
            let nextSibling = grabbed.nextElementSibling;
            grabbed.parentElement.removeChild(grabbed);
            if (nextSibling.nextElementSibling) {
              nextSibling = nextSibling.nextElementSibling;
              nextSibling.parentElement.insertBefore(grabbed, nextSibling);
            } else {
              nextSibling.parentElement.appendChild(grabbed);
            }
            grabbed.querySelector("[aria-pressed='true']").focus();
            thisEditor.relatedItems.resetIndexes(thisEditor.lastChild);
            setStatus(
              "Moved down to position " + (getElementIndex(grabbed) + 1)
            );
          }
        }
      }
    });
  });
})();
