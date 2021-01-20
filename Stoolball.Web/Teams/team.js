window.addEventListener("DOMContentLoaded", function (event) {
  // If there's a complete list of competitions hidden on initial load, add a button to show it instead of the shorter list
  const allCompetitions = document.querySelector(".season-list__all");
  if (allCompetitions) {
    const showAllCompetitions = document.createElement("button");
    showAllCompetitions.setAttribute("type", "button");
    showAllCompetitions.appendChild(
      document.createTextNode("Show all competitions")
    );
    showAllCompetitions.classList.add("btn");
    showAllCompetitions.classList.add("btn-secondary");
    showAllCompetitions.classList.add("btn-show");
    showAllCompetitions.classList.add("d-print-none");

    showAllCompetitions.addEventListener("click", function () {
      allCompetitions.classList.remove("d-none");
      showAllCompetitions.parentNode.removeChild(showAllCompetitions);

      const currentSeasons = document.querySelector(".season-list__current");
      if (currentSeasons) {
        currentSeasons.parentNode.removeChild(currentSeasons);
      }
    });

    allCompetitions.parentNode.insertBefore(
      showAllCompetitions,
      allCompetitions
    );
  }
});
