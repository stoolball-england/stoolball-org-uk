"use strict";

// For Jest tests
if (typeof module !== "undefined" && typeof module.exports !== "undefined") {
  module.exports = createBowlingScorecardEditor;
}

function createBowlingScorecardEditor() {
  return {
    ordinalSuffixOf: function (i) {
      var j = i % 10,
        k = i % 100;
      if (j == 1 && k != 11) {
        return i + "st";
      }
      if (j == 2 && k != 12) {
        return i + "nd";
      }
      if (j == 3 && k != 13) {
        return i + "rd";
      }
      return i + "th";
    },
  };
}

(function () {
  window.addEventListener("DOMContentLoaded", function () {
    const editor = document.querySelector(".bowling-scorecard-editor");
    if (!editor) {
      return;
    }

    const bowlingScorecardEditor = createBowlingScorecardEditor();

    const overClass = "bowling-scorecard-editor__over";
    const ordinalBowlerLabelClass = "bowling-scorecard-editor__bowler-label";
    const bowlerInputClass = "scorecard__player-name";
    const ballsInputClass = "scorecard__balls";
    const widesInputClass = "scorecard__wides";
    const noBallsInputClass = "scorecard__no-balls";
    const runsInputClass = "scorecard__runs";

    function disableFollowingRows(row) {
      while (row.nextElementSibling) {
        let thisPlayer = row.querySelector("." + bowlerInputClass);
        let nextPlayer = row.nextElementSibling.querySelector(
          "." + bowlerInputClass
        );
        if (!thisPlayer.value && !nextPlayer.value) {
          nextPlayer.setAttribute("disabled", "disabled");
        }
        row = row.nextElementSibling;
      }
    }

    function focusEvent(e) {
      if (e.target.classList.contains(bowlerInputClass)) {
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
      const row = e.target.closest("tr");
      const playerField = row.querySelector("." + bowlerInputClass);
      if (playerField.value.trim().length === 0) return;

      // If this over was bowled, previous overs must have been, so set balls bowled to 8 if missing
      let previous = row.previousElementSibling,
        previousPlayer,
        previousBalls;
      while (previous) {
        previousPlayer = previous.querySelector("." + bowlerInputClass);
        if (previousPlayer.value.trim().length > 0) {
          previousBalls = previous.querySelector("." + ballsInputClass);
          if (previousBalls.value.trim().length === 0) {
            previousBalls.value = 8;
          }
        }
        previous = previous.previousElementSibling;
      }

      // // Apply defaults to current field and previous fields (previous fields useful for touch screens)
      const ballsField = row.querySelector("." + ballsInputClass);
      const widesField = row.querySelector("." + widesInputClass);
      const noballsField = row.querySelector("." + noBallsInputClass);

      // Balls bowled defaults to 8, extras default to 0
      if (e.target.classList.contains(ballsInputClass)) {
        e.target.value = 8;
      } else if (e.target.classList.contains(noBallsInputClass)) {
        if (ballsField.value.trim().length === 0) {
          ballsField.value = 8;
        }
        e.target.value = 0;
      } else if (e.target.classList.contains(widesInputClass)) {
        if (ballsField.value.trim().length === 0) {
          ballsField.value = 8;
        }
        if (noballsField.value.trim().length === 0) {
          noballsField.value = 0;
        }
        e.target.value = 0;
      } else if (e.target.classList.contains(runsInputClass)) {
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
      let row = e.target.closest("tr");
      row = row.previousElementSibling;
      if (!row) return;
      row = row.previousElementSibling;
      if (!row) return;
      const twoUp = row.querySelector("." + bowlerInputClass).value;

      // If four rows above is different, the bowling is probably shared around
      let fourUp;
      row = row.previousElementSibling;
      if (row) {
        row = row.previousElementSibling;
        if (row) {
          fourUp = row.querySelector("." + bowlerInputClass).value;
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
    addOver.setAttribute(
      "class",
      "btn btn-secondary btn-add bowling-scorecard-editor__add-over"
    );
    addOver.setAttribute("disabled", "disabled");
    addOver.appendChild(document.createTextNode("Add an over"));
    const addOverContainer = document.createElement("div");
    addOverContainer.setAttribute(
      "class",
      "bowling-scorecard-editor__add-over-wrapper"
    );
    addOverContainer.appendChild(addOver);
    editor.parentElement.insertBefore(
      addOverContainer,
      editor.nextElementSibling
    );

    function enableOverEvent(e) {
      if (e.target && e.target.classList.contains(bowlerInputClass)) {
        enableOver(e.target.closest("tr"));
      }
    }

    function enableOver(tableRow) {
      const playerName = tableRow.querySelector("." + bowlerInputClass);
      const fields = tableRow.querySelectorAll("input[type=number]");

      if (playerName.value) {
        for (let f = 0; f < fields.length; f++) {
          fields[f].removeAttribute("disabled");
        }

        // If this over row used, ensure next one is ready
        if (tableRow.nextElementSibling) {
          tableRow.nextElementSibling
            .querySelector("." + bowlerInputClass)
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

    function blurEvent(e) {
      tryDeleteRow(e);
      showFullNameHint(e);
    }

    function tryDeleteRow(e) {
      if (!e.target.classList.contains(bowlerInputClass)) {
        return;
      }

      if (e.target.value.trim()) {
        return;
      }

      const row = e.target.closest("tr");
      const nextRowHasBowler =
        row.nextElementSibling &&
        row.nextElementSibling
          .querySelector("." + bowlerInputClass)
          .value.trim();
      if (nextRowHasBowler) {
        updateIndexesOfFollowingRows(row);

        // Set a class on the row so that CSS can transition it, then delete it after the transition
        row.classList.add(overClass + "--deleted");
        row.addEventListener("transitionend", function () {
          if (row.parentElement) {
            row.parentElement.removeChild(row);
          }
        });
      } else {
        resetOver(row);
      }
    }

    function updateIndexesOfFollowingRows(tr) {
      while (
        tr.nextElementSibling &&
        tr.nextElementSibling.classList.contains(overClass)
      ) {
        let target = tr.nextElementSibling;
        const index =
          [].slice.call(tr.parentElement.children).indexOf(target) - 1;
        target.querySelector("th[scope='row']").id =
          "over-header--" + index + "--";
        target.querySelector("." + ordinalBowlerLabelClass).innerHTML =
          bowlingScorecardEditor.ordinalSuffixOf(index + 1) + " bowler";
        [].slice
          .call(target.querySelectorAll("[aria-labelledby]"))
          .map(function (element) {
            element.setAttribute(
              "aria-labelledby",
              element
                .getAttribute("aria-labelledby")
                .replace(/--[0-9]+--/, "--" + index + "--")
            );
          });
        [].slice.call(target.querySelectorAll("[for]")).map(function (element) {
          element.setAttribute(
            "for",
            element.getAttribute("for").replace(/_[0-9]+__/, "_" + index + "__")
          );
        });
        [].slice.call(target.querySelectorAll("[id]")).map(function (element) {
          element.id = element.id.replace(/_[0-9]+__/, "_" + index + "__");
        }).id;
        [].slice
          .call(target.querySelectorAll("[name]"))
          .map(function (element) {
            element.setAttribute(
              "name",
              element
                .getAttribute("name")
                .replace(/\[[0-9]+]/, "[" + index + "]")
            );
          });
        tr = tr.nextElementSibling;
      }
    }

    function resetOver(tr) {
      const inputs = tr.querySelectorAll("input");
      [].forEach.call(inputs, function (input) {
        input.value = "";
      });
    }

    function showFullNameHint(e) {
      if (!e.target.classList.contains(bowlerInputClass)) {
        return;
      }

      // querySelectorAll to get players, and slice to convert that to an array.
      // map to get the input.value from the input, and filter to get unique values,
      // then filter again to get one-word names
      const threeOrMoreOneWordNames =
        [].slice
          .call(document.querySelectorAll("." + bowlerInputClass))
          .map(function (x) {
            return x.value;
          })
          .filter(function (value, index, self) {
            return self.indexOf(value.trim()) === index;
          })
          .filter(function (x) {
            return x && x.indexOf(" ") === -1;
          }).length >= 3;

      const hint = document.querySelector(".bowling-scorecard__full-name-tip");
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
    editor.addEventListener("focusout", blurEvent);

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
      const fields = newRow.querySelectorAll("input,select,label,th");
      const attributes = ["name", "for", "id", "aria-labelledby"];
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

      for (let i = 0; i < fields.length; i++) {
        for (let j = 0; j < attributes.length; j++) {
          let value = fields[i].getAttribute(attributes[j]);
          value = value.replace(/\[[0-9]+\]/, "[" + (rowNumber - 1) + "]");
          value = value.replace(/--[0-9]+--/, "--" + (rowNumber - 1) + "--");
          if (value) {
            fields[i].setAttribute(attributes[j], value);
          }
        }

        if (fields[i].tagName === "LABEL") {
          fields[i].innerText = fields[i].innerText.replace(
            /\[[0-9]+th\]/,
            ordinal
          );
        }
      }
      enableOver(newRow);

      if (typeof stoolball.autocompletePlayer !== "undefined") {
        stoolball.autocompletePlayer(fields[0]);
      }

      // focus the first field
      fields[0].focus();
    });
  });
})();
