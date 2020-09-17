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
    // var batTotal = $("#batTotal");
    // if (batTotal.length > 0 && batTotal[0].value.length == 0) {
    //   batTotal[0].calculateRuns = true;
    //   batTotal.blur(function () {
    //     if ($.trim(this.value).length > 0) this.calculateRuns = false;
    //   });
    //   calculateRuns({
    //     target: batTotal[0],
    //   });
    //   $("input.runs").blur(calculateRuns);
    // }

    // function calculateRuns(e) {
    //   // Determine whether we want to calculate runs automatically
    //   if (
    //     typeof e.target.calculateRuns != "undefined" ||
    //     $(e.target).hasClass("runs")
    //   ) {
    //     var batTotal = document.getElementById("batTotal");
    //     if (!batTotal.calculateRuns) return;

    //     // Add up the runs in all the run boxes, including extras
    //     var calculatedTotal = 0;
    //     $(".batting input.runs:not(:disabled)").each(function () {
    //       var runs = parseInt(this.value);
    //       if (!isNaN(runs)) calculatedTotal += runs;
    //     });

    //     // Update the total, with an animation of the calculation
    //     var currentTotal = parseInt(batTotal.value);
    //     if (isNaN(currentTotal)) currentTotal = 0;
    //     if (calculatedTotal == 0) {
    //       batTotal.value = "";
    //     } else if (calculatedTotal > currentTotal) {
    //       var timeOut = 0;
    //       for (var i = currentTotal + 1; i <= calculatedTotal; i++) {
    //         currentTotal++;
    //         setTimeout(
    //           "document.getElementById('batTotal').value = " + currentTotal,
    //           timeOut
    //         );
    //         timeOut += 15;
    //       }
    //     } else if (calculatedTotal < currentTotal) {
    //       var timeOut = 0;
    //       for (var i = currentTotal - 1; i >= calculatedTotal; i--) {
    //         currentTotal--;
    //         setTimeout(
    //           "document.getElementById('batTotal').value = " + currentTotal,
    //           timeOut
    //         );
    //         timeOut += 15;
    //       }
    //     }
    //   }
    // }

    // Calculate wickets total
    // var batWickets = $("#batWickets");
    // if (batWickets.length > 0 && batWickets[0].selectedIndex == 0) {
    //   batWickets[0].calculateWickets = true;
    //   batWickets.blur(function () {
    //     if (this.selectedIndex > 0) this.calculateWickets = false;
    //   });
    //   calculateWickets({
    //     target: batWickets[0],
    //   });
    //   $("select.howOut").blur(calculateWickets);
    // }

    // function calculateWickets(e) {
    //   // Determine whether we want to calculate wickets automatically
    //   if (
    //     typeof e.target.calculateWickets != "undefined" ||
    //     $(e.target).hasClass("howOut")
    //   ) {
    //     var batWickets = document.getElementById("batWickets");
    //     if (!batWickets.calculateWickets) return;

    //     // Set wickets taken, but if 0 set to unknown to avoid risk of
    //     // stating 0 when that's not known
    //     var wickets = $(
    //       "select.howOut :selected[value=9],select.howOut :selected[value=10],select.howOut :selected[value=4],select.howOut :selected[value=5],select.howOut :selected[value=6],select.howOut :selected[value=7],select.howOut :selected[value=8]"
    //     ).length;
    //     if (wickets > 0) {
    //       // If wickets == number of batsmen-1, that's all out (in outdoor
    //       // stoolball)
    //       var allOut =
    //         batWickets.parentNode.parentNode.parentNode.childNodes.length - 7;
    //       if (wickets == allOut) {
    //         $(batWickets).val(-1);
    //       } else {
    //         $(batWickets).val(wickets);
    //       }
    //     } else {
    //       batWickets.selectedIndex = 0;
    //     }
    //   }
    // }

    function enableBattingRowEvent(e) {
      if (e.target && e.target.classList.contains("scorecard__player-name")) {
        enableBattingRow(e.target.parentElement.parentElement);
      }
      if (e.target.tagName === "SELECT") {
        howOutEnableDetails(e.target.parentElement.parentElement);
      }
    }

    // Auto enable/disable scorecard fields
    // .change event fires when the field is clicked in Chrome
    editor.addEventListener("keyup", enableBattingRowEvent);
    editor.addEventListener("click", enableBattingRowEvent);
    editor.addEventListener("change", enableBattingRowEvent);

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
      .querySelector(".extras")
      .parentElement.insertBefore(addBatterTr, editor.querySelector(".extras"));
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
