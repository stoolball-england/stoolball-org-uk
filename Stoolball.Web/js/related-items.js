(function () {
  function resetIndexes(selectedItem) {
    /* Reset the indexes on the remaining fields so that ASP.NET model binding reads them all */
    const remainingData = selectedItem.parentNode.querySelectorAll(
      ".related-item__data"
    );

    let index = -1;
    let dataItem;
    for (let i = 0; i < remainingData.length; i++) {
      if (remainingData[i].getAttribute("data-item") !== dataItem) {
        index++;
        dataItem = remainingData[i].getAttribute("data-item");
      }

      remainingData[i].setAttribute(
        "name",
        remainingData[i]
          .getAttribute("name")
          .replace(/\[[0-9]+\]/, "[" + index + "]")
      );
    }
  }

  function resetAutocompleteParams(selectedItem) {
    const existingIdFields = selectedItem.parentNode.querySelectorAll(
      ".related-item__id"
    );
    const existingIds = [];
    for (let j = 0; j < existingIdFields.length; j++) {
      existingIds.push(existingIdFields[j].value);
    }
    return { not: existingIds };
  }

  window.addEventListener("DOMContentLoaded", function () {
    const relatedItems = document.querySelectorAll(".related-items");
    for (let i = 0; i < relatedItems.length; i++) {
      relatedItems[i].addEventListener("click", function (e) {
        /* Get a consistent target of the selected item container element, or null if it wasn't the delete button clicked */
        const className = "related-item__delete";
        const selectedItem = e.target.parentNode.parentNode.classList.contains(
          className
        )
          ? e.target.parentNode.parentNode.parentNode
          : null;

        if (selectedItem) {
          /* Stop the link from activating and the event from bubbling up further */
          e.preventDefault();
          e.stopPropagation();

          /* Remove any data fields so that the item isn't posted */
          const dataFields = selectedItem.querySelectorAll(
            ".related-item__data"
          );
          for (let j = 0; j < dataFields.length; j++) {
            dataFields[j].parentNode.removeChild(dataFields[j]);
          }

          resetIndexes(selectedItem);

          /* Reset autocomplete options so the deleted team is available for reselection */
          const searchField = selectedItem.parentNode.querySelector(
            ".related-item__search"
          );
          $(searchField)
            .autocomplete()
            .setOptions({ params: resetAutocompleteParams(selectedItem) });

          /* Set a class on the selected item container element so that CSS can transition it, then delete it after the transition */
          selectedItem.classList.add("related-item__deleted");
          selectedItem.addEventListener("transitionend", function () {
            selectedItem.parentNode &&
              selectedItem.parentNode.removeChild(selectedItem);
          });

          /* Create an alert for assistive technology */
          var alert = document.createElement("div");
          alert.setAttribute("role", "alert");
          alert.setAttribute("class", "sr-only");
          alert.appendChild(document.createTextNode("Item removed"));
          document.body.appendChild(alert);

          /* Set the focus to the search field */
          searchField.focus();
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
      const selectedItem = searchField.parentNode.parentNode;
      const params = resetAutocompleteParams(selectedItem);

      $(searchField).autocomplete({
        serviceUrl: url,
        params: params,
        onSelect: function (suggestion) {
          selectedItem.insertAdjacentHTML(
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

          resetIndexes(selectedItem);

          /* Reset autocomplete options to the added team is excluded from further suggestions */
          $(this)
            .autocomplete()
            .setOptions({ params: resetAutocompleteParams(selectedItem) });

          /* Clear the search field */
          this.value = "";
        },
      });
    }
  });
})();
