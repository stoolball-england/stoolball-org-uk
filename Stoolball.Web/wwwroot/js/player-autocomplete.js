if (typeof stoolball === "undefined") {
  stoolball = {};
}
stoolball.autocompletePlayer = function (input) {
  const thisMatch = "this match";
  stoolball.autocompletePlayer.currentFocus = "";

  // Cache suggestions locally to help recognise any name not already suggested as a new player
  if (!stoolball.autocompletePlayer.cachedSuggestions) {
    stoolball.autocompletePlayer.cachedSuggestions = [];
  }

  // Cache new players first mentioned on this page
  if (!stoolball.autocompletePlayer.newPlayers) {
    stoolball.autocompletePlayer.newPlayers = [];
  }

  function cacheValueByTeam(cache, teamId, cachedValue) {
    if (!cache[teamId]) {
      cache[teamId] = [];
    }
    if (cache[teamId].indexOf(cachedValue) === -1) {
      cache[teamId].push(cachedValue);
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
    // Disable the built-in cache because names added to the new players list are not added to cached results, so
    // are missing from later lookups on the same page.
    noCache: true,
    containerClass: "autocomplete-suggestions player-suggestions",
    params: { teams: input.getAttribute("data-team").split(",").join("&teams=") },
    formatResult: function (suggestion, currentValue) {
      // As the result is displayed, separate the value to be selected from the metadata informing its selection.
      // Cache the suggested value so that we can check on blur whether the entered value was one of the suggestions.
      const targetTeam = input.getAttribute("data-team");
      if (suggestion.data.playerRecord !== thisMatch) {
        cacheValueByTeam(
          stoolball.autocompletePlayer.cachedSuggestions,
          targetTeam,
          suggestion.value
        );
      }
      return (
        $.Autocomplete.defaults.formatResult(suggestion, currentValue) +
        " (" +
        suggestion.data.playerRecord +
        ")"
      );
    },
    transformResult: function (response, originalQuery) {
      // If the user has moved on before the results are returned, don't return the results as the dropdown
      // can obscure other fields causing a serious usability problem.
      if (input.id !== stoolball.autocompletePlayer.currentFocus) {
        return { suggestions: [] };
      }

      response = JSON.parse(response);

      // Find new players first mentioned on this page, who match the search term.
      const targetTeam = input.getAttribute("data-team");
      const newPlayerSuggestions = stoolball.autocompletePlayer.newPlayers[
        targetTeam
      ]
        ? stoolball.autocompletePlayer.newPlayers[targetTeam].filter(function (
            value
          ) {
            return RegExp(originalQuery.replace(/\s/, ".*"), "gi").test(value);
          })
        : [];

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

  // When the field blurs a name has been chosen, so add to an internal cache if it's a new player
  input.addEventListener("blur", function (e) {
    const playerName = capitalise(this.value.trim());
    const teamId = e.target.getAttribute("data-team");
    if (!isSuggestedPlayer(teamId, this.value)) {
      cacheValueByTeam(
        stoolball.autocompletePlayer.newPlayers,
        teamId,
        playerName
      );
    }
  });
};

(function () {
  window.addEventListener("DOMContentLoaded", function () {
    const targetFields = document.querySelectorAll(".scorecard__player-name");

    for (let i = 0; i < targetFields.length; i++) {
      stoolball.autocompletePlayer(targetFields[i]);
    }

    // Track current focus so we know if we're still in the field the lookup was for
    document.body.addEventListener("focusin", function (e) {
      stoolball.autocompletePlayer.currentFocus = e.target.id;
    });
  });
})();
