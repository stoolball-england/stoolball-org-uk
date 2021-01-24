"use strict";

(function () {
  window.addEventListener("DOMContentLoaded", function () {
    const progress = document.querySelector("progress");
    if (progress) {
      const updateUrl = progress.getAttribute("data-url");
      const updater = setInterval(updateProgress, 1000);

      function updateProgress() {
        fetch(updateUrl, { credentials: "same-origin" }).then(function (
          response
        ) {
          if (response.ok) {
            response.json().then(function (value) {
              if (Number(value.percent) === 100) {
                clearInterval(updater);

                const elementsToRemove = progress.parentNode.querySelectorAll(
                  ".while-progress"
                );
                for (let i = 0; i < elementsToRemove.length; i++) {
                  elementsToRemove[i].parentNode.removeChild(
                    elementsToRemove[i]
                  );
                }

                const report = document.createElement("p");
                report.setAttribute("class", "alert alert-info");
                report.setAttribute("role", "alert");
                const errorReport = value.errors
                  ? '<span class="text-danger"' +
                    value.errors +
                    " errors.</span>"
                  : "";
                report.innerHTML =
                  "<strong>100% complete." + errorReport + "</strong>";
                progress.replaceWith(report);
              }
              progress.setAttribute("value", value.percent);
              progress.innerHTML = value.percent + "%";
            });
          }
        });
      }
    }
  });
})();
