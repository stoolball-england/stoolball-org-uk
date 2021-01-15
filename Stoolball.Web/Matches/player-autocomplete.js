if (typeof stoolball === "undefined") {
  stoolball = {};
}
stoolball.autocompletePlayer = function (input) {
  const thisMatch = "this match";

  // Cache suggestions locally to help recognise any name not already suggested as a new player
  if (!stoolball.autocompletePlayer.cachedSuggestions) {
    stoolball.autocompletePlayer.cachedSuggestions = [];
  }

  function cacheSuggestedPlayer(teamId, suggestion) {
    if (!stoolball.autocompletePlayer.cachedSuggestions[teamId]) {
      stoolball.autocompletePlayer.cachedSuggestions[teamId] = [];
    }
    if (
      stoolball.autocompletePlayer.cachedSuggestions[teamId].indexOf(
        suggestion.value
      ) === -1
    ) {
      stoolball.autocompletePlayer.cachedSuggestions[teamId].push(
        suggestion.value
      );
    }
  }

  function isSuggestedPlayer(teamId, player) {
    return (
      player &&
      stoolball.autocompletePlayer.cachedSuggestions[teamId] &&
      stoolball.autocompletePlayer.cachedSuggestions[teamId].indexOf(player) >
        -1
    );
  }

  function capitalise(name) {
    function capitaliseSegment(x) {
      return x.length > 1 &&
        ["de", "la", "di", "da", "della", "van", "von"].indexOf(x) == -1
        ? x.charAt(0).toUpperCase() + x.substr(1)
        : x;
    }
    return name
      .replace(/\s/g, " ")
      .split(" ")
      .map(capitaliseSegment)
      .join(" ")
      .split("-")
      .map(capitaliseSegment)
      .join("-");
  }

  $(input).autocomplete({
    serviceUrl: "/api/players/autocomplete",
    // Disable the built-in cache because the metadata separated in formatResult() can go missing
    // when you select a suggestion then press backspace and get the suggestions again.
    noCache: true,
    containerClass: "autocomplete-suggestions player-suggestions",
    params: { teams: input.getAttribute("data-team").split(",") },
    formatResult: function (suggestion, currentValue) {
      // As the result is displayed, separate the value to be selected from the metadata informing its selection.
      // Cache the suggested value so that we can check on blur whether the entered value was one of the suggestions.
      const targetTeam = input.getAttribute("data-team");
      if (suggestion.data.playerRecord !== thisMatch) {
        cacheSuggestedPlayer(targetTeam, suggestion);
      }
      return (
        $.Autocomplete.defaults.formatResult(suggestion, currentValue) +
        " (" +
        suggestion.data.playerRecord +
        ")"
      );
    },
    transformResult: function (response, originalQuery) {
      response = JSON.parse(response);

      // querySelectorAll to get new players, and slice to convert that to an array.
      // map to get the input.value from the input, and filter to get unique values,
      // then filter again to get values matching the search term.
      const newPlayerSuggestions = [].slice
        .call(
          document.querySelectorAll(
            ".scorecard__player-name[data-new-player='true']"
          )
        )
        .map(function (x) {
          return x.value;
        })
        .filter(function (value, index, self) {
          return self.indexOf(value.trim()) === index;
        })
        .filter(function (value) {
          return RegExp(originalQuery.replace(/\s/, ".*"), "gi").test(value);
        });

      // Add any matching new players to the start of the suggestions list.
      for (let i = 0; i < newPlayerSuggestions.length; i++) {
        response.suggestions.unshift({
          value: newPlayerSuggestions[i],
          data: {
            playerRecord: thisMatch,
          },
        });
      }
      return response;
    },
  });

  // When the field blurs a name has been chosen, so set an attribute to record whether it's a new player
  input.addEventListener("blur", function (e) {
    this.value = capitalise(this.value.trim());
    this.setAttribute(
      "data-new-player",
      !isSuggestedPlayer(e.target.getAttribute("data-team"), this.value)
    );
  });
};

(function () {
  window.addEventListener("DOMContentLoaded", function () {
    const targetFields = document.querySelectorAll(".scorecard__player-name");

    for (let i = 0; i < targetFields.length; i++) {
      stoolball.autocompletePlayer(targetFields[i]);
    }
  });
})();
