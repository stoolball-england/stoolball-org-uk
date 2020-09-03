(function () {
  window.addEventListener("DOMContentLoaded", function () {
    const editor = document.getElementsByClassName(
      "bowling-scorecard-editor"
    )[0];

    function disableFollowingRows(tr) {
      while (tr.nextElementSibling) {
        var thisPlayer = tr.querySelector(".scorecard__player-name");
        var nextPlayer = tr.nextElementSibling.querySelector(
          ".scorecard__player-name"
        );
        if (!thisPlayer.value && !nextPlayer.value) {
          nextPlayer.setAttribute("disabled", "disabled");
        }
        tr = tr.nextElementSibling;
      }
    }

    // Suggest defaults for bowling stats
    //   function suggestBowlingDefaults(e) {
    //     var thisField = $(e.target);

    //     // Is this field empty?
    //     if ($.trim(e.target.value).length > 0) {
    //       thisField.select();
    //       return;
    //     }

    //     // Is there a player?
    //     var tableRow = e.target.parentNode.parentNode;
    //     var playerField = tableRow.firstChild.firstChild;
    //     if ($.trim(playerField.value).length == 0) return;

    //     // If this over was bowled, previous overs must have been, so set balls bowled to 8 if missing
    //     var previous = tableRow.previousSibling,
    //       previousPlayer,
    //       previousBalls;
    //     while (previous) {
    //       previousPlayer = previous.firstChild.firstChild;
    //       if ($.trim(previousPlayer.value).length > 0) {
    //         previousBalls = previous.childNodes[1].firstChild;
    //         if ($.trim(previousBalls.value).length == 0) {
    //           previousBalls.value = 8;
    //         }
    //       }
    //       previous = previous.previousSibling;
    //     }

    //     // Apply defaults to current field and previous fields (previous fields useful for touch screens)
    //     var ballsField = tableRow.childNodes[1].firstChild;
    //     var noballsField = tableRow.childNodes[2].firstChild;
    //     var widesField = tableRow.childNodes[3].firstChild;

    //     // Balls bowled defaults to 8, extras default to 0
    //     if (thisField.hasClass("balls")) {
    //       e.target.value = 8;
    //     } else if (thisField.hasClass("no-balls")) {
    //       if ($.trim(ballsField.value).length == 0) {
    //         ballsField.value = 8;
    //       }
    //       e.target.value = 0;
    //     } else if (thisField.hasClass("wides")) {
    //       if ($.trim(ballsField.value).length == 0) {
    //         ballsField.value = 8;
    //       }
    //       if ($.trim(noballsField.value).length == 0) {
    //         noballsField.value = 0;
    //       }
    //       e.target.value = 0;
    //     } else if (thisField.hasClass("runs")) {
    //       if ($.trim(ballsField.value).length == 0) {
    //         ballsField.value = 8;
    //       }
    //       if ($.trim(noballsField.value).length == 0) {
    //         noballsField.value = 0;
    //       }
    //       if ($.trim(widesField.value).length == 0) {
    //         widesField.value = 0;
    //       }
    //     }
    //     thisField.select();
    //   }

    //   function replaceBowlingDefaults(e) {
    //     // Best fix for bug which means Mobile Safari doesn't select text.
    //     // This replaces default value when a number is typed, even if it's not selected.
    //     if (e.keyCode >= 49 && e.keyCode <= 57) {
    //       if (e.target.value == "0") e.target.value = "";
    //     }
    //   }

    // More often than not the same bowler bowled two overs ago, so copy the name
    //   function suggestDefaultBowler() {
    //     // Is this field empty?
    //     if ($.trim(this.value).length > 0) return;

    //     // Get player name from two rows above
    //     var tableRow = this.parentNode.parentNode;
    //     tableRow = tableRow.previousSibling;
    //     if (!tableRow) return;
    //     tableRow = tableRow.previousSibling;
    //     if (!tableRow) return;
    //     var twoUp = tableRow.firstChild.firstChild.value;

    //     // If four rows above is different, the bowling is probably shared around
    //     var fourUp;
    //     tableRow = tableRow.previousSibling;
    //     if (tableRow) {
    //       tableRow = tableRow.previousSibling;
    //       if (tableRow) {
    //         fourUp = tableRow.firstChild.firstChild.value;
    //       }
    //     }

    //     if (!fourUp || fourUp === twoUp) {
    //       this.value = twoUp;
    //       this.select();
    //     }
    //   }

    // Add over button
    const addOver = document.createElement("a");
    addOver.setAttribute("href", "#");
    addOver.setAttribute("class", "btn btn-secondary add-over");
    //addOver.setAttribute("disabled", "disabled");
    addOver.appendChild(document.createTextNode("Add an over"));
    editor.parentElement.insertBefore(addOver, editor.nextElementSibling);

    function enableOverEvent(e) {
      if (e.target && e.target.classList.contains("scorecard__player-name")) {
        enableOver(e.target.parentElement.parentElement);
      }
    }

    function enableOver(tableRow) {
      let playerName = tableRow.getElementsByClassName(
        "scorecard__player-name"
      )[0];
      const fields = tableRow.querySelectorAll("input[type=number]");

      if (playerName.value) {
        for (let f = 0; f < fields.length; f++) {
          fields[f].removeAttribute("disabled");
        }

        // If this over row used, ensure next one is ready
        if (tableRow.nextElementSibling) {
          tableRow.nextElementSibling
            .getElementsByClassName("scorecard__player-name")[0]
            .removeAttribute("disabled");
        } else {
          document
            .getElementsByClassName("add-over")[0]
            .removeAttribute("disabled");
        }
      } else {
        for (let f = 0; f < fields.length; f++) {
          fields[f].setAttribute("disabled", "disabled");
        }

        // If this over row not used, disable the following ones to reduce tabbing
        if (tableRow.nextElementSibling) {
          disableFollowingRows(tableRow);
        } else {
          document
            .getElementsByClassName("add-over")[0]
            .setAttribute("disabled", "disabled");
        }
      }
    }

    // Auto enable/disable scorecard fields
    // .change event fires when the field is clicked in Chrome
    editor.addEventListener("keyup", enableOverEvent);
    editor.addEventListener("click", enableOverEvent);
    editor.addEventListener("change", enableOverEvent);

    // Run enableOver for every row to setup the fields on page load
    const overs = editor.querySelectorAll("tbody > tr");
    for (let i = 0; i < overs.length; i++) {
      enableOver(overs[i]);
    }

    //   $("input.numeric", ".bowling-scorecard-editor")
    //     .focus(suggestBowlingDefaults)
    //     .keydown(replaceBowlingDefaults);
    //   $("input.player", ".bowling-scorecard-editor").focus(
    //     suggestDefaultBowler
    //   );

    // Focus first field
    editor.querySelector("input[type='text']:not(:disabled)").focus();

    addOver.addEventListener("click", function (e) {
      e.preventDefault();

      // insert a new row based on a template
      const rows = editor
        .getElementsByTagName("tbody")[0]
        .getElementsByTagName("tr");
      let template = document.getElementById("over-template").innerHTML;
      rows[rows.length - 1].insertAdjacentHTML("afterend", template);

      // update the index of the new row
      let newRow = rows[rows.length - 1].nextElementSibling;
      const inputs = newRow.querySelectorAll("input");
      for (let i = 0; i < inputs.length; i++) {
        inputs[i].setAttribute(
          "name",
          inputs[i]
            .getAttribute("name")
            .replace(/\[[0-9]+\]/, "[" + rows.length + "]")
        );
      }
      enableOverEvent({ target: inputs[0] }); // TODO: Needs work!

      // focus the first field
      newRow.querySelector("input").focus();

      //   $("input.numeric", newRow)
      //     .focus(suggestBowlingDefaults)
      //     .keydown(replaceBowlingDefaults);
      //   if (typeof stoolballAutoSuggest != "undefined")
      //     $("input.player", newRow).each(
      //       stoolballAutoSuggest.enablePlayerSuggestions
      //     );
      //   updateTotalOvers();
    });

    //   function updateTotalOvers() {
    //     $("#bowlerRows").val($(".bowling-scorecard input.player").length);
    //   }
  });
})();
