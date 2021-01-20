"use strict";

(function () {
  window.addEventListener("DOMContentLoaded", function () {
    const overs = document.querySelectorAll(".bowling-scorecard.overs");
    for (let i = 0; i < overs.length; i++) {
      overs[i].classList.add("d-none");
      const caption = overs[i].querySelector("caption").innerText;

      const button = document.createElement("button");
      button.setAttribute("type", "button");
      button.setAttribute("class", "btn btn-secondary btn-show");
      button.appendChild(document.createTextNode("Show " + caption));
      button.addEventListener("click", function () {
        this.parentElement.removeChild(this);
        overs[i].classList.remove("d-none");
      });

      overs[i].insertAdjacentElement("afterend", button);
    }
  });
})();
