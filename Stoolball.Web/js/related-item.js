(function () {
  window.addEventListener("DOMContentLoaded", function () {
    const relatedItems = document.querySelectorAll(".related-item");
    for (let i = 0; i < relatedItems.length; i++) {
      relatedItems[i].addEventListener("click", function (e) {
        /* Get a consistent target of the selected item container element, or null if it wasn't the delete button clicked */
        const className = "related-item__delete";
        const selectedItem = e.target.parentNode.classList.contains(className)
          ? e.target.parentNode.parentNode
          : null;

        if (selectedItem) {
          /* Stop the link from activating and the event from bubbling up further */
          e.preventDefault();
          e.stopPropagation();

          /* Create an alert for assistive technology */
          var alert = document.createElement("div");
          alert.setAttribute("role", "alert");
          alert.setAttribute("class", "sr-only");
          alert.appendChild(document.createTextNode("Item removed"));
          document.body.appendChild(alert);

          /* Set the focus to the search field */
          const searchField = selectedItem.parentNode.querySelector(
            ".related-item__search"
          );
          searchField.style.display = "block";
          searchField.removeAttribute("disabled");
          searchField.focus();

          /* Remove the deleted item */
          selectedItem.parentNode &&
            selectedItem.parentNode.removeChild(selectedItem);
        }
      });

      const searchField = relatedItems[i].querySelector(
        ".related-item__search"
      );
      searchField.addEventListener("keypress", function (e) {
        // Prevent enter submitting the form within this editor
        if (e.keyCode === 13) {
          e.preventDefault();
        }
      });

      let url = searchField.getAttribute("data-url");
      let template = document.getElementById(
        searchField.getAttribute("data-template")
      ).innerHTML;

      $(searchField).autocomplete({
        serviceUrl: url,
        params: {},
        onSelect: function (suggestion) {
          this.insertAdjacentHTML(
            "beforebegin",
            template
              .replace(/{{value}}/g, suggestion.value)
              .replace(/{{data}}/g, suggestion.data)
          );

          /* Create an alert for assistive technology */
          var alert = document.createElement("div");
          alert.setAttribute("role", "alert");
          alert.setAttribute("class", "sr-only");
          alert.appendChild(
            document.createTextNode("Added " + suggestion.value)
          );
          document.body.appendChild(alert);

          /* Clear and hide the search field */
          this.value = "";
          this.style.display = "none";
          this.setAttribute("disabled", "disabled");
        },
      });
    }
  });
})();
