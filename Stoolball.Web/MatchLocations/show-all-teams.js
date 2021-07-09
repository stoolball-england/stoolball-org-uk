window.addEventListener("DOMContentLoaded", function (event) {
  // If there's a complete list of teams hidden on initial load, add a button to show it instead of the shorter list
  const allTeams = document.querySelector(".team-list__all");
  if (allTeams) {
    const showAllTeams = document.createElement("button");
    showAllTeams.setAttribute("type", "button");
    showAllTeams.appendChild(
      document.createTextNode("Show past teams")
    );
    showAllTeams.classList.add("btn");
    showAllTeams.classList.add("btn-secondary");
    showAllTeams.classList.add("btn-show");
    showAllTeams.classList.add("d-print-none");

    showAllTeams.addEventListener("click", function () {
      allTeams.classList.remove("d-none");
      showAllTeams.parentNode.removeChild(showAllTeams);

      const currentTeams = document.querySelector(".team-list__current");
      if (currentTeams) {
        currentTeams.parentNode.removeChild(currentTeams);
      }
    });

    allTeams.parentNode.insertBefore(
      showAllTeams,
      allTeams
    );
  }
});
