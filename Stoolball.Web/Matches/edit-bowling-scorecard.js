(function () {
  window.addEventListener("DOMContentLoaded", function () {
    const editor = document.querySelector(".bowling-scorecard-editor");

    function disableFollowingRows(row) {
      while (row.nextElementSibling) {
        let thisPlayer = row.querySelector(".scorecard__player-name");
        let nextPlayer = row.nextElementSibling.querySelector(
          ".scorecard__player-name"
        );
        if (!thisPlayer.value && !nextPlayer.value) {
          nextPlayer.setAttribute("disabled", "disabled");
        }
        row = row.nextElementSibling;
      }
    }

    function focusEvent(e) {
      if (e.target.classList.contains("scorecard__player-name")) {
        suggestDefaultBowler(e);
      }
      if (e.target.getAttribute("type") === "number") {
        suggestBowlingDefaults(e);
      }
    }
    // Suggest defaults for bowling stats
    function suggestBowlingDefaults(e) {
      // Is this field empty?
      if (e.target.value.trim().length > 0) {
        e.target.select();
        return;
      }

      // Is there a player?
      const row = e.target.parentNode.parentNode;
      const playerField = row.querySelector(".scorecard__player-name");
      if (playerField.value.trim().length === 0) return;

      // If this over was bowled, previous overs must have been, so set balls bowled to 8 if missing
      let previous = row.previousElementSibling,
        previousPlayer,
        previousBalls;
      while (previous) {
        previousPlayer = previous.querySelector(".scorecard__player-name");
        if (previousPlayer.value.trim().length > 0) {
          previousBalls = previous.querySelector(".scorecard__balls");
          if (previousBalls.value.trim().length === 0) {
            previousBalls.value = 8;
          }
        }
        previous = previous.previousElementSibling;
      }

      // // Apply defaults to current field and previous fields (previous fields useful for touch screens)
      const ballsField = row.querySelector(".scorecard__balls");
      const widesField = row.querySelector(".scorecard__wides");
      const noballsField = row.querySelector(".scorecard__no-balls");

      // Balls bowled defaults to 8, extras default to 0
      if (e.target.classList.contains("scorecard__balls")) {
        e.target.value = 8;
      } else if (e.target.classList.contains("scorecard__no-balls")) {
        if (ballsField.value.trim().length === 0) {
          ballsField.value = 8;
        }
        e.target.value = 0;
      } else if (e.target.classList.contains("scorecard__wides")) {
        if (ballsField.value.trim().length === 0) {
          ballsField.value = 8;
        }
        if (noballsField.value.trim().length === 0) {
          noballsField.value = 0;
        }
        e.target.value = 0;
      } else if (e.target.classList.contains("scorecard__runs")) {
        if (ballsField.value.trim().length === 0) {
          ballsField.value = 8;
        }
        if (noballsField.value.trim().length === 0) {
          noballsField.value = 0;
        }
        if (widesField.value.trim().length === 0) {
          widesField.value = 0;
        }
      }
      e.target.select();
    }

    function replaceBowlingDefaults(e) {
      // Best fix for bug which means Mobile Safari doesn't select text.
      // This replaces default value when a number is typed, even if it's not selected.
      if (e.target.getAttribute("type") !== "number") return;
      if (e.keyCode >= 49 && e.keyCode <= 57) {
        if (e.target.value === "0") e.target.value = "";
      }
    }

    // More often than not the same bowler bowled two overs ago, so copy the name
    function suggestDefaultBowler(e) {
      // Is this field empty?
      if (e.target.value.trim().length > 0) return;

      // Get player name from two rows above
      let row = e.target.parentNode.parentNode;
      row = row.previousElementSibling;
      if (!row) return;
      row = row.previousElementSibling;
      if (!row) return;
      const twoUp = row.querySelector(".scorecard__player-name").value;

      // If four rows above is different, the bowling is probably shared around
      let fourUp;
      row = row.previousElementSibling;
      if (row) {
        row = row.previousElementSibling;
        if (row) {
          fourUp = row.querySelector(".scorecard__player-name").value;
        }
      }

      if (!fourUp || fourUp === twoUp) {
        e.target.value = twoUp;
        e.target.select();
      }
    }

    // Add over button
    const addOver = document.createElement("button");
    addOver.setAttribute("type", "button");
    addOver.setAttribute("class", "btn btn-secondary");
    addOver.setAttribute("disabled", "disabled");
    addOver.appendChild(document.createTextNode("Add an over"));
    editor.parentElement.insertBefore(addOver, editor.nextElementSibling);

    function enableOverEvent(e) {
      if (e.target && e.target.classList.contains("scorecard__player-name")) {
        enableOver(e.target.parentElement.parentElement);
      }
    }

    function enableOver(tableRow) {
      const playerName = tableRow.querySelector(".scorecard__player-name");
      const fields = tableRow.querySelectorAll("input[type=number]");

      if (playerName.value) {
        for (let f = 0; f < fields.length; f++) {
          fields[f].removeAttribute("disabled");
        }

        // If this over row used, ensure next one is ready
        if (tableRow.nextElementSibling) {
          tableRow.nextElementSibling
            .querySelector(".scorecard__player-name")
            .removeAttribute("disabled");
        } else {
          addOver.removeAttribute("disabled");
        }
      } else {
        for (let f = 0; f < fields.length; f++) {
          fields[f].setAttribute("disabled", "disabled");
        }

        // If this over row not used, disable the following ones to reduce tabbing
        if (tableRow.nextElementSibling) {
          disableFollowingRows(tableRow);
        } else {
          addOver.setAttribute("disabled", "disabled");
        }
      }
    }

    function showFullNameHint(e) {
      if (!e.target.classList.contains("scorecard__player-name")) {
        return;
      }

      // querySelectorAll to get players, and slice to convert that to an array.
      // map to get the input.value from the input, and filter to get unique values,
      // then filter again to get one-word names
      const threeOrMoreOneWordNames =
        [].slice
          .call(document.querySelectorAll(".scorecard__player-name"))
          .map(function (x) {
            return x.value;
          })
          .filter(function (value, index, self) {
            return self.indexOf(value.trim()) === index;
          })
          .filter(function (x) {
            return x && x.indexOf(" ") === -1;
          }).length >= 3;

      const hint = document.querySelector(".scorecard__full-name-hint");
      if (threeOrMoreOneWordNames) {
        hint.classList.remove("d-none");
        hint.classList.add("d-block");
      } else {
        hint.classList.add("d-none");
        hint.classList.remove("d-block");
      }
    }

    // Auto enable/disable scorecard fields
    // .change event fires when the field is clicked in Chrome
    editor.addEventListener("keyup", enableOverEvent);
    editor.addEventListener("click", enableOverEvent);
    editor.addEventListener("change", enableOverEvent);
    editor.addEventListener("focusin", focusEvent);
    editor.addEventListener("keydown", replaceBowlingDefaults);
    editor.addEventListener("focusout", showFullNameHint);

    // Run enableOver for every row to setup the fields on page load
    const overs = editor.querySelectorAll("tbody > tr");
    for (let i = 0; i < overs.length; i++) {
      enableOver(overs[i]);
    }

    // Focus first field
    if (editor.getAttribute("data-autofocus") !== "false") {
      editor.querySelector("input[type='text']:not(:disabled)").focus();
    }

    addOver.addEventListener("click", function (e) {
      e.preventDefault();

      // insert a new row based on a template
      const rows = editor.querySelectorAll("tbody > tr");
      const template = document.getElementById("over-template").innerHTML;
      rows[rows.length - 1].insertAdjacentHTML("afterend", template);

      // update the index of the new row
      let newRow = rows[rows.length - 1].nextElementSibling;
      const inputs = newRow.querySelectorAll("input");
      for (let i = 0; i < inputs.length; i++) {
        let ordinal,
          rowNumber = rows.length + 1;
        switch (rowNumber % 10) {
          case 1:
            ordinal = rowNumber === 11 ? rowNumber + "th" : rowNumber + "st";
            break;
          case 2:
            ordinal = rowNumber === 12 ? rowNumber + "th" : rowNumber + "nd";
            break;
          case 3:
            ordinal = rowNumber === 13 ? rowNumber + "th" : rowNumber + "rd";
            break;
          default:
            ordinal = rowNumber + "th";
        }

        inputs[i].setAttribute(
          "name",
          inputs[i]
            .getAttribute("name")
            .replace(/\[[0-9]+\]/, "[" + rows.length + "]")
        );

        const ordinalAttributes = [
          "data-msg-number",
          "data-msg-min",
          "data-msg-max",
        ];
        for (let j = 0; j < ordinalAttributes.length; j++) {
          let value = inputs[i].getAttribute(ordinalAttributes[j]);
          if (value) {
            inputs[i].setAttribute(
              ordinalAttributes[j],
              value.replace(/\[[0-9]+th\]/, ordinal)
            );
          }
        }
      }
      enableOver(newRow);

      if (typeof stoolball.autocompletePlayer !== "undefined") {
        stoolball.autocompletePlayer(inputs[0]);
      }

      // focus the first field
      inputs[0].focus();
    });
  });
})();
