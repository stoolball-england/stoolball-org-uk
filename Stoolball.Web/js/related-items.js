(function () {
  function resetIndexes(tableRow) {
    /* Reset the indexes on the remaining fields so that ASP.NET model binding reads them all */
    const remainingData = tableRow.parentNode.querySelectorAll(
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

  function resetAutocompleteParams(tableRow) {
    const existingIdFields = tableRow.parentNode.querySelectorAll(
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
        /* Get a consistent target of the table row, or null if it wasn't the delete button clicked */
        const className = "related-item__delete";
        const tableRow = e.target.parentNode.parentNode.classList.contains(
          className
        )
          ? e.target.parentNode.parentNode.parentNode
          : null;

        if (tableRow) {
          /* Stop the link from activating and the event from bubbling up further */
          e.preventDefault();
          e.stopPropagation();

          /* Remove any data fields so that the item isn't posted */
          const dataFields = tableRow.querySelectorAll(".related-item__data");
          for (let j = 0; j < dataFields.length; j++) {
            dataFields[j].parentNode.removeChild(dataFields[j]);
          }

          resetIndexes(tableRow);

          /* Reset autocomplete options so the deleted team is available for reselection */
          $(tableRow.parentNode.querySelector(".related-item__search"))
            .autocomplete()
            .setOptions({ params: resetAutocompleteParams(tableRow) });

          /* Set a class on the table row so that CSS can transition it */
          tableRow.classList.add("related-item__deleted");
        }
      });
    }

    const searchFields = document.querySelectorAll(".related-item__search");
    for (let i = 0; i < searchFields.length; i++) {
      let url = searchFields[i].getAttribute("data-url");
      let template = document.getElementById(
        searchFields[i].getAttribute("data-template")
      ).innerHTML;
      const tableRow = searchFields[i].parentNode.parentNode;
      const params = resetAutocompleteParams(tableRow);

      $(searchFields[i]).autocomplete({
        serviceUrl: url,
        params: params,
        onSelect: function (suggestion) {
          tableRow.insertAdjacentHTML(
            "beforebegin",
            template
              .replace(/{{value}}/g, suggestion.value)
              .replace(/{{data}}/g, suggestion.data)
          );
          resetIndexes(tableRow);

          /* Reset autocomplete options to the added team is excluded from further suggestions */
          $(this)
            .autocomplete()
            .setOptions({ params: resetAutocompleteParams(tableRow) });

          /* Clear the search field */
          this.value = "";
        },
      });
    }
  });
})();
