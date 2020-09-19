(function () {
  window.addEventListener("DOMContentLoaded", function () {
    const editor = document.querySelector(".batting-scorecard-editor");

    function enableBattingRow(tr) {
      const batter = tr.querySelector(".scorecard__player-name");
      const howOut = tr.querySelector("select");
      if (batter.value) {
        howOut.removeAttribute("disabled");
        // If this batting row is used, ensure next one is ready
        if (
          tr.nextElementSibling &&
          tr.nextElementSibling.classList.contains("scorecard__batter")
        ) {
          const nextBatter = tr.nextElementSibling.querySelector(
            ".scorecard__player-name"
          );
          nextBatter.removeAttribute("disabled");
        } else {
          editor
            .querySelector(".edit-batting-scorecard__add-batter button")
            .removeAttribute("disabled");
        }
      } else {
        howOut.setAttribute("disabled", "disabled");
        // If this batting row not used, disable the following ones to reduce tabbing
        if (
          tr.nextElementSibling &&
          tr.nextElementSibling.classList.contains("scorecard__batter")
        ) {
          disableFollowingRows(tr);
        } else {
          editor
            .querySelector(".edit-batting-scorecard__add-batter button")
            .setAttribute("disabled", "disabled");
        }
      }
    }

    function disableFollowingRows(tr) {
      while (
        tr.nextElementSibling &&
        tr.nextElementSibling.classList.contains("scorecard__batter")
      ) {
        const thisBatter = tr.querySelector(".scorecard__player-name");
        const nextBatter = tr.nextElementSibling.querySelector(
          ".scorecard__player-name"
        );
        if (!thisBatter.value && !nextBatter.value) {
          nextBatter.setAttribute("disabled", "disabled");
        }
        tr = tr.nextElementSibling;
      }
    }

    function howOutEnableDetails(tr) {
      let enableDismissedBy = true,
        enableBowler = true,
        enableRuns = true;
      const howOut = tr.querySelector("select");
      switch (howOut.value) {
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

      const dismissedBy = tr.querySelectorAll(".scorecard__player-name")[1];
      if (enableDismissedBy) {
        dismissedBy.removeAttribute("disabled");
      } else {
        dismissedBy.setAttribute("disabled", "disabled");
      }

      const bowler = tr.querySelectorAll(".scorecard__player-name")[2];
      if (enableBowler) {
        bowler.removeAttribute("disabled");
      } else {
        bowler.setAttribute("disabled", "disabled");
      }

      const runs = tr.querySelector(".scorecard__runs");
      const balls = tr.querySelector(".scorecard__balls");
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
      const inputs = editor.querySelectorAll(".scorecard__runs:not(:disabled)");
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

    const total = editor.querySelector(".scorecard__total");
    let calculateInningsRuns = false;
    if (!total.value) {
      calculateInningsRuns = true;
      calculateRuns();
    }

    // Calculate wickets total
    const wickets = editor.querySelector(".scorecard__wickets");
    let calculateWickets = false;

    if (wickets.selectedIndex === 0) {
      calculateWickets = true;
      calculateInningsWickets();
    }

    function calculateInningsWickets() {
      // Set wickets taken, but if 0 set to unknown to avoid risk of
      // stating 0 when that's not known
      const totalWickets = editor.querySelectorAll(
        ".scorecard__dismissal :checked[value='Bowled'],.scorecard__dismissal :checked[value='CaughtAndBowled'],.scorecard__dismissal :checked[value='BodyBeforeWicket'],.scorecard__dismissal :checked[value='RunOut'],.scorecard__dismissal :checked[value='HitTheBallTwice'],.scorecard__dismissal :checked[value='TimedOut'],.scorecard__dismissal :checked[value='Caught']"
      ).length;
      if (totalWickets > 0) {
        wickets.querySelector("[value='" + totalWickets + "']").selected = true;
      } else {
        wickets.selectedIndex = 0;
      }
    }

    function enableBattingRowEvent(e) {
      if (e.target && e.target.classList.contains("scorecard__player-name")) {
        enableBattingRow(e.target.parentElement.parentElement);
      }
      if (e.target.classList.contains("scorecard__dismissal")) {
        howOutEnableDetails(e.target.parentElement.parentElement);
      }
    }

    function blurEvent(e) {
      // Determine whether we want to calculate runs and wickets automatically

      if (e.target.classList.contains("scorecard__total")) {
        if (e.target.value.trim()) {
          calculateInningsRuns = false;
        }
      }

      if (e.target.classList.contains("scorecard__wickets")) {
        if (e.target.selectedIndex > 0) {
          calculateWickets = false;
        }
      }

      if (
        (e.target.classList.contains("scorecard__runs") ||
          e.target === total) &&
        calculateInningsRuns
      ) {
        calculateRuns();
      }

      if (
        e.target.classList.contains("scorecard__dismissal") &&
        calculateWickets
      ) {
        calculateInningsWickets();
      }

      if (e.target.classList.contains("scorecard__player-name")) {
        showFullNameHint();
      }
    }

    function showFullNameHint() {
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
    editor.addEventListener("keyup", enableBattingRowEvent);
    editor.addEventListener("click", enableBattingRowEvent);
    editor.addEventListener("change", enableBattingRowEvent);
    editor.addEventListener("focusout", blurEvent);

    // Add batter button
    const addBatterTr = document.createElement("tr");
    addBatterTr.classList.add("edit-batting-scorecard__add-batter");
    const addBatterTd = document.createElement("td");
    addBatterTd.setAttribute("colspan", "6");
    addBatterTr.appendChild(addBatterTd);
    const addBatter = document.createElement("button");
    addBatter.setAttribute("class", "btn btn-secondary");
    addBatter.setAttribute("disabled", "disabled");
    addBatter.appendChild(document.createTextNode("Add a batter"));
    addBatterTd.appendChild(addBatter);
    editor
      .querySelector(".scorecard__extras")
      .parentElement.insertBefore(addBatterTr, editor.querySelector(".scorecard__extras"));
    addBatter.addEventListener("click", function (e) {
      e.preventDefault();
      var lastRow = addBatterTr.previousElementSibling;
      const template = document.getElementById("innings-template").innerHTML;
      lastRow.insertAdjacentHTML("afterend", template);

      // update the index of the new row
      let newRow = lastRow.nextElementSibling;
      const fields = newRow.querySelectorAll("input,select");
      for (let i = 0; i < fields.length; i++) {
        let ordinal,
          rowNumber = editor.querySelectorAll("tr[class='scorecard__batter']")
            .length;
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

        fields[i].setAttribute(
          "name",
          fields[i]
            .getAttribute("name")
            .replace(/\[[0-9]+\]/, "[" + (rowNumber - 1) + "]")
        );

        const ordinalAttributes = ["data-msg-number", "data-msg-min"];
        for (let j = 0; j < ordinalAttributes.length; j++) {
          let value = fields[i].getAttribute(ordinalAttributes[j]);
          if (value) {
            fields[i].setAttribute(
              ordinalAttributes[j],
              value.replace(/\[[0-9]+th\]/, ordinal)
            );
          }
        }
      }

      if (typeof stoolball.autocompletePlayer !== "undefined") {
        stoolball.autocompletePlayer(fields[0]);
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
      "tr[class='scorecard__batter']"
    );
    for (let i = 0; i < battingRows.length; i++) {
      enableBattingRow(battingRows[i]);
      howOutEnableDetails(battingRows[i]);
    }

    // focus the first field
    editor.querySelector("input[type='text']:not(:disabled)").focus();
  });
})();
