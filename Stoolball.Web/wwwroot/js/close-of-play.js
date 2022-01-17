(function () {
  window.addEventListener("DOMContentLoaded", function () {
    const yes = document.querySelector("#match-went-ahead-yes");
    const no = document.querySelector("#match-went-ahead-no");
    const yesFields = document.querySelector("#match-went-ahead-yes-fields");
    const noFields = document.querySelector("#match-went-ahead-no-fields");
    if (!yes || !no || !yesFields || !noFields) {
      return;
    }

    function yesNoChanged(e) {
      if (e.target.value === "True") {
        yesFields.style.display = "block";
        noFields.style.display = "none";
      } else {
        yesFields.style.display = "none";
        noFields.style.display = "block";
      }
    }

    if (yes.checked) {
      noFields.style.display = "none";
    } else if (no.checked) {
      yesFields.style.display = "none";
    } else {
      yesFields.style.display = noFields.style.display = "none";
    }
    yes.addEventListener("change", yesNoChanged);
    no.addEventListener("change", yesNoChanged);
  });
})();
