(function () {
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

          /* Remove the id field so that it's not posted */
          const id = tableRow.querySelector(".related-item__id");
          if (id) {
            id.parentNode.removeChild(id);
          }

          /* Set a class on the table row so that CSS can transition it */
          tableRow.classList.add("related-item__deleted");
        }
      });
    }
  });
})();
