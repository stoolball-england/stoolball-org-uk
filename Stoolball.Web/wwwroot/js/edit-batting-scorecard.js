"use strict";

// For Jest tests
if (typeof module !== "undefined" && typeof module.exports !== "undefined") {
  module.exports = createBattingScorecardEditor;
}

function createBattingScorecardEditor() {
  return {
    /**
     * Adds an ordinal suffix to a given number
     * @param {number} i
     * @returns string
     */
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
    const editor = document.querySelector(".batting-scorecard-editor");
    if (!editor) {
      return;
    }

    const battingScorecardEditor = createBattingScorecardEditor();

    const playerInningsRowClass = "batting-scorecard-editor__player-innings";
    const ordinalBatterLabelClass = "batting-scorecard-editor__batter-label";
    const playerNameFieldClass = "scorecard__player-name";
    const batterFieldClass = "scorecard__batter";
    const dismissalTypeFieldClass = "scorecard__dismissal";
    const runsFieldClass = "scorecard__runs";
    const wicketsFieldClass = "scorecard__wickets";
    const totalRunsFieldClass = "scorecard__total";
    const ballsFacedFieldClass = "scorecard__balls";
    const addBatterButtonClass = "batting-scorecard-editor__add-batter";
    const enterFullNamesTipClass = "batting-scorecard__full-name-tip";

    function enableBattingRow(tr) {
      const batter = tr.querySelector("." + playerNameFieldClass);
      const dismissalType = tr.querySelector("." + dismissalTypeFieldClass);
      if (batter.value) {
        if (dismissalType) {
          dismissalType.removeAttribute("disabled");
        }
        // If this batting row is used, ensure next one is ready
        if (
          tr.nextElementSibling &&
          tr.nextElementSibling.classList.contains(playerInningsRowClass)
        ) {
          const nextBatter = tr.nextElementSibling.querySelector(
            "." + playerNameFieldClass
          );
          nextBatter.removeAttribute("disabled");
        } else {
          editor
            .querySelector("." + addBatterButtonClass)
            .removeAttribute("disabled");
        }
      } else {
        if (dismissalType) {
          dismissalType.setAttribute("disabled", "disabled");
        }
        // If this batting row not used, disable the following ones to reduce tabbing
        if (
          tr.nextElementSibling &&
          tr.nextElementSibling.classList.contains(playerInningsRowClass)
        ) {
          disableFollowingRows(tr);
        } else {
          editor
            .querySelector("." + addBatterButtonClass)
            .setAttribute("disabled", "disabled");
        }
      }
    }

    function resetBattingRow(tr) {
      const dismissalType = tr.querySelector("." + dismissalTypeFieldClass);
      if (dismissalType) {
        dismissalType.selectedIndex = 0;
      }
      const inputs = tr.querySelectorAll("input");
      [].forEach.call(inputs, function (input) {
        input.value = "";
      });
    }

    function disableFollowingRows(tr) {
      while (
        tr.nextElementSibling &&
        tr.nextElementSibling.classList.contains(playerInningsRowClass)
      ) {
        const thisBatter = tr.querySelector("." + playerNameFieldClass);
        const nextBatter = tr.nextElementSibling.querySelector(
          "." + playerNameFieldClass
        );
        if (!thisBatter.value && !nextBatter.value) {
          nextBatter.setAttribute("disabled", "disabled");
        }
        tr = tr.nextElementSibling;
      }
    }

    function dismissalTypeEnableDetails(tr) {
      let enableDismissedBy = true,
        enableBowler = true,
        enableRuns = true;
      const dismissalType = tr.querySelector("." + dismissalTypeFieldClass);
      switch (dismissalType.value) {
        case "DidNotBat":
        case "TimedOut":
          enableDismissedBy = false;
          enableBowler = false;
          enableRuns = false;
          break;
        case "NotOut":
        case "Retired":
        case "RetiredHurt":
        case "": // unknown dismissal
          enableDismissedBy = false;
          enableBowler = false;
          break;
        case "Bowled":
        case "CaughtAndBowled":
        case "BodyBeforeWicket":
        case "HitTheBallTwice":
          enableDismissedBy = false;
          break;
        case "RunOut":
          enableBowler = false;
          break;
      }

      const dismissedBy = tr.querySelectorAll("." + playerNameFieldClass)[1];
      if (enableDismissedBy) {
        dismissedBy.removeAttribute("disabled");
      } else {
        dismissedBy.setAttribute("disabled", "disabled");
      }

      const bowler = tr.querySelectorAll("." + playerNameFieldClass)[2];
      if (enableBowler) {
        bowler.removeAttribute("disabled");
      } else {
        bowler.setAttribute("disabled", "disabled");
      }

      const runs = tr.querySelector("." + runsFieldClass);
      const balls = tr.querySelector("." + ballsFacedFieldClass);
      if (enableRuns) {
        runs.removeAttribute("disabled");
        balls.removeAttribute("disabled");
      } else {
        runs.setAttribute("disabled", "disabled");
        balls.setAttribute("disabled", "disabled");
      }
    }

    // Calculate batting total
    function calculateRuns() {
      // Add up the runs in all the run boxes, including extras
      let calculatedTotal = 0;
      const inputs = editor.querySelectorAll(
        "." + runsFieldClass + ":not(:disabled)"
      );
      for (let i = 0; i < inputs.length; i++) {
        let runs = parseInt(inputs[i].value);
        if (!isNaN(runs)) {
          calculatedTotal += runs;
        }
      }

      // Update the total, with an animation of the calculation
      let currentTotal = parseInt(total.value);
      if (isNaN(currentTotal)) {
        currentTotal = 0;
      }
      if (calculatedTotal === 0) {
        total.value = "";
      } else if (calculatedTotal > currentTotal) {
        let timeOut = 0;
        for (let i = currentTotal + 1; i <= calculatedTotal; i++) {
          currentTotal++;
          setTimeout(
            (function (value) {
              return function () {
                total.value = value;
              };
            })(currentTotal),
            timeOut
          );
          timeOut += 15;
        }
      } else if (calculatedTotal < currentTotal) {
        let timeOut = 0;
        for (let i = currentTotal - 1; i >= calculatedTotal; i--) {
          currentTotal--;
          setTimeout(
            (function (value) {
              return function () {
                total.value = value;
              };
            })(currentTotal),
            timeOut
          );
          timeOut += 15;
        }
      }
    }

    const total = editor.querySelector("." + totalRunsFieldClass);
    let calculateInningsRuns = false;
    if (!total.value) {
      calculateInningsRuns = true;
      calculateRuns();
    }

    // Calculate wickets total
    const wickets = editor.querySelector("." + wicketsFieldClass);
    let calculateWickets = false;

    if (wickets.selectedIndex === 0) {
      calculateWickets = true;
      calculateInningsWickets();
    }

    function calculateInningsWickets() {
      // Set wickets taken, but if 0 set to unknown to avoid risk of
      // stating 0 when that's not known
      const totalWickets = editor.querySelectorAll(
        [
          "Bowled",
          "CaughtAndBowled",
          "BodyBeforeWicket",
          "RunOut",
          "HitTheBallTwice",
          "TimedOut",
          "Caught",
        ]
          .map(function (dismissalType) {
            return (
              "." +
              dismissalTypeFieldClass +
              " :checked[value='" +
              dismissalType +
              "']"
            );
          })
          .join(",")
      ).length;
      if (totalWickets > 0) {
        wickets.querySelector("[value='" + totalWickets + "']").selected = true;
      } else {
        wickets.selectedIndex = 0;
      }
    }

    function enableBattingRowEvent(e) {
      if (e.target && e.target.classList.contains(playerNameFieldClass)) {
        enableBattingRow(e.target.closest("tr"));
      }
      if (e.target.classList.contains(dismissalTypeFieldClass)) {
        dismissalTypeEnableDetails(e.target.closest("tr"));
      }
    }

    function blurEvent(e) {
      // Determine whether we want to calculate runs and wickets automatically

      if (e.target.classList.contains(totalRunsFieldClass)) {
        if (e.target.value.trim()) {
          calculateInningsRuns = false;
        }
      }

      if (e.target.classList.contains(wicketsFieldClass)) {
        if (e.target.selectedIndex > 0) {
          calculateWickets = false;
        }
      }

      if (
        (e.target.classList.contains(runsFieldClass) || e.target === total) &&
        calculateInningsRuns
      ) {
        calculateRuns();
      }

      if (
        e.target.classList.contains(dismissalTypeFieldClass) &&
        calculateWickets
      ) {
        calculateInningsWickets();
      }

      if (e.target.classList.contains(playerNameFieldClass)) {
        showFullNameHint();
      }

      if (
        e.target.classList.contains(batterFieldClass) &&
        !e.target.value.trim()
      ) {
        const row = e.target.closest("tr");
        const nextRowHasBatter =
          row.nextElementSibling &&
          row.nextElementSibling
            .querySelector(".scorecard__batter")
            .value.trim();
        if (nextRowHasBatter) {
          updateIndexesOfFollowingRows(row);

          // Set a class on the row so that CSS can transition it, then delete it after the transition
          row.classList.add(
            "batting-scorecard-editor__player-innings--deleted"
          );
          row.addEventListener("transitionend", function () {
            if (row.parentElement) {
              row.parentElement.removeChild(row);
            }
          });
        } else {
          resetBattingRow(row);
        }
      }
    }

    function updateIndexesOfFollowingRows(tr) {
      while (
        tr.nextElementSibling &&
        tr.nextElementSibling.classList.contains(playerInningsRowClass)
      ) {
        let target = tr.nextElementSibling;
        const index =
          [].slice.call(tr.parentElement.children).indexOf(target) - 1;
        target.querySelector("th[scope='row']").id =
          "player-innings-header--" + index + "--";
        target.querySelector("." + ordinalBatterLabelClass).innerHTML =
          battingScorecardEditor.ordinalSuffixOf(index + 1) + " batter";
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

    function showFullNameHint() {
      // querySelectorAll to get players, and slice to convert that to an array.
      // map to get the input.value from the input, and filter to get unique values,
      // then filter again to get one-word names
      const threeOrMoreOneWordNames =
        [].slice
          .call(document.querySelectorAll("." + playerNameFieldClass))
          .map(function (x) {
            return x.value;
          })
          .filter(function (value, index, self) {
            return self.indexOf(value.trim()) === index;
          })
          .filter(function (x) {
            return x && x.indexOf(" ") === -1;
          }).length >= 3;

      const hint = document.querySelector("." + enterFullNamesTipClass);
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
    editor.addEventListener("keyup", enableBattingRowEvent);
    editor.addEventListener("click", enableBattingRowEvent);
    editor.addEventListener("change", enableBattingRowEvent);
    editor.addEventListener("focusout", blurEvent);

    // Add batter button
    const addBatterTr = document.createElement("tr");
    addBatterTr.classList.add(addBatterButtonClass + "-wrapper");
    const addBatterTd = document.createElement("td");
    addBatterTd.setAttribute("colspan", "6");
    addBatterTd.setAttribute("class", addBatterButtonClass + "-wrapper");
    addBatterTr.appendChild(addBatterTd);
    const addBatter = document.createElement("button");
    addBatter.setAttribute("type", "button");
    addBatter.setAttribute(
      "class",
      "btn btn-secondary btn-add " + addBatterButtonClass
    );
    addBatter.setAttribute("disabled", "disabled");
    addBatter.appendChild(document.createTextNode("Add a batter"));
    addBatterTd.appendChild(addBatter);
    editor
      .querySelector("." + playerInningsRowClass + ":last-child")
      .parentElement.appendChild(addBatterTr);
    addBatter.addEventListener("click", function (e) {
      e.preventDefault();
      var lastRow = addBatterTr.previousElementSibling;
      const template = document.getElementById("innings-template").innerHTML;
      lastRow.insertAdjacentHTML("afterend", template);

      // update the index of the new row
      let newRow = lastRow.nextElementSibling;
      const fields = newRow.querySelectorAll("input,select,label,th");
      const attributes = ["name", "for", "id", "aria-labelledby"];
      for (let i = 0; i < fields.length; i++) {
        let ordinal,
          rowNumber = editor.querySelectorAll(
            "tr[class='" + playerInningsRowClass + "']"
          ).length;
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

        for (let j = 0; j < attributes.length; j++) {
          let value = fields[i].getAttribute(attributes[j]);
          if (value) {
            fields[i].setAttribute(
              attributes[j],
              value.replace(/\[[0-9]+\]/, "[" + (rowNumber - 1) + "]")
            );
            fields[i].setAttribute(
              attributes[j],
              value.replace(/--[0-9]+--/, "--" + (rowNumber - 1) + "--")
            );
          }
        }

        if (fields[i].tagName === "LABEL") {
          fields[i].innerText = fields[i].innerText.replace(
            /\[[0-9]+th\]/,
            ordinal
          );
        }
      }

      if (typeof stoolball.autocompletePlayer !== "undefined") {
        const playerNameFields = newRow.querySelectorAll(
          "." + playerNameFieldClass
        );
        for (let i = 0; i < playerNameFields.length; i++) {
          stoolball.autocompletePlayer(playerNameFields[i]);
        }
      }

      // also need to allow for an extra wicket
      const lastWicket = wickets.querySelector("option:last-child");
      const lastWicketValue = parseInt(lastWicket.getAttribute("value"));

      const newWicket = document.createElement("option");
      newWicket.setAttribute("value", lastWicketValue.toString());
      newWicket.appendChild(
        document.createTextNode(lastWicketValue.toString())
      );
      lastWicket.parentElement.insertBefore(newWicket, lastWicket);

      lastWicket.setAttribute("value", (lastWicketValue + 1).toString());

      // focus the first field
      fields[0].focus();
    });

    // Run enableBattingRow for every row to setup the fields on page load
    const battingRows = editor.querySelectorAll(
      "tr[class='" + playerInningsRowClass + "']"
    );
    for (let i = 0; i < battingRows.length; i++) {
      enableBattingRow(battingRows[i]);
      dismissalTypeEnableDetails(battingRows[i]);
    }

    // focus the first field
    if (editor.getAttribute("data-autofocus") !== "false") {
      editor.querySelector("input[type='text']:not(:disabled)").focus();
    }
  });
})();
