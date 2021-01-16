"use strict";

// For Jest tests
if (typeof module !== "undefined" && typeof module.exports !== "undefined") {
  module.exports = createRelatedItemsEditor;
}

function createRelatedItemsEditor() {
  return {
    populateTemplate: function (template, suggestion) {
      const uuid = this.uuidv4();
      return template.replace(/{{\s*([a-z.]+)\s*}}/gi, function (match, token) {
        if (token === "value") {
          return suggestion.value;
        }
        if (token === "data") {
          return suggestion.data && suggestion.data.hasOwnProperty("data")
            ? suggestion.data.data
            : suggestion.data;
        }
        if (token.indexOf("data.") === 0) {
          const prop = token.substring(5);
          if (suggestion.data.hasOwnProperty(prop)) {
            return suggestion.data[prop];
          }
        }
        if (token === "create") {
          return suggestion.data && suggestion.data.hasOwnProperty("create")
            ? suggestion.data.create
              ? "Yes"
              : "No"
            : "";
        }
        if (token === "id") {
          return uuid;
        }
        return match;
      });
    },

    /* Creates a new GUID https://stackoverflow.com/questions/105034/how-to-create-guid-uuid */
    uuidv4: function () {
      return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(
        /[xy]/g,
        function (c) {
          var r = (Math.random() * 16) | 0,
            v = c == "x" ? r : (r & 0x3) | 0x8;
          return v.toString(16);
        }
      );
    },

    resetEmpty: function (editor) {
      const selectedItems = editor.querySelectorAll(".related-item__selected");
      if (selectedItems.length) {
        editor.classList.remove("related-items__empty");
      } else {
        editor.classList.add("related-items__empty");
      }
    },
  };
}
(function () {
  const editorUtilities = createRelatedItemsEditor();

  function resetIndexes(selectedItem) {
    /* Reset the indexes on the remaining fields so that ASP.NET model binding reads them all */
    const remainingData = selectedItem.parentNode.querySelectorAll(
      ".related-item__data, .related-items-as-cards__label"
    );

    let index = -1;
    let dataItem;
    for (let i = 0; i < remainingData.length; i++) {
      if (remainingData[i].getAttribute("data-item") !== dataItem) {
        index++;
        dataItem = remainingData[i].getAttribute("data-item");
      }

      replaceIndex(remainingData[i], "name", index);
      replaceIndex(remainingData[i], "for", index);
      replaceIndex(remainingData[i], "id", index);
      replaceIndex(remainingData[i], "aria-describedby", index);

      const validator = remainingData[i].parentNode.querySelector(
        ".field-validation-valid"
      );
      if (validator) {
        replaceIndex(validator, "id", index);
        replaceIndex(validator, "data-valmsg-for", index);
      }
    }
  }

  function replaceIndex(element, attribute, index) {
    const currentValue = element.getAttribute(attribute);
    if (currentValue) {
      element.setAttribute(
        attribute,
        currentValue.replace(/\[[0-9]+\]/, "[" + index + "]")
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

  function findSelectedItemForDelete(target) {
    while (target !== null && target.parentNode !== null) {
      if (target.classList.contains("related-item__delete")) {
        return target.parentNode;
      }
      target = target.parentNode;
    }
  }

  window.addEventListener("DOMContentLoaded", function () {
    const relatedItems = document.querySelectorAll(".related-items");
    for (let i = 0; i < relatedItems.length; i++) {
      let thisEditor = relatedItems[i];
      editorUtilities.resetEmpty(thisEditor);
      thisEditor.addEventListener("click", function (e) {
        /* Get a consistent target of the selected item container element, or null if it wasn't the delete button clicked */
        const selectedItem = findSelectedItemForDelete(e.target);

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
            editorUtilities.resetEmpty(thisEditor);
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

      const searchField = thisEditor.querySelector(".related-item__search");

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
        triggerSelectOnValidInput: false,
        onSelect: function (suggestion) {
          selectedItem.insertAdjacentHTML(
            "beforebegin",
            editorUtilities.populateTemplate(template, suggestion)
          );

          /* Create an alert for assistive technology */
          const itemType = thisEditor.getAttribute("data-related-item")
            ? thisEditor.getAttribute("data-related-item")
            : "item";

          var alert = document.createElement("div");
          alert.setAttribute("role", "alert");
          alert.setAttribute("class", "sr-only");
          alert.appendChild(
            document.createTextNode("Added " + suggestion.value)
          );
          if (suggestion.data && suggestion.data.create) {
            alert.appendChild(
              document.createTextNode(
                "This new " + itemType + " will be created."
              )
            );
          }
          document.body.appendChild(alert);

          resetEmpty(thisEditor);
          resetIndexes(selectedItem);

          /* Reset autocomplete options to the added team is excluded from further suggestions */
          $(this)
            .autocomplete()
            .setOptions({ params: resetAutocompleteParams(selectedItem) });

          /* Clear the search field */
          this.value = "";
        },
        groupBy: thisEditor.classList.contains("related-items__create")
          ? "category"
          : null,
        transformResult: function (response, originalQuery) {
          response = JSON.parse(response);
          if (thisEditor.classList.contains("related-items__create")) {
            const itemType = thisEditor.getAttribute("data-related-item")
              ? thisEditor.getAttribute("data-related-item")
              : "item";
            response.suggestions = response.suggestions.map(function (x) {
              return {
                value: x.value,
                data: {
                  create: false,
                  category: "Pick a current " + itemType,
                  data: x.data,
                },
              };
            });
            response.suggestions.push({
              value: originalQuery,
              data: {
                create: true,
                category: "Add a new " + itemType,
                data: editorUtilities.uuidv4(),
              },
            });
          }

          const valueTemplate = searchField.getAttribute("data-value-template");
          if (valueTemplate) {
            response.suggestions = response.suggestions.map(function (x) {
              return {
                value: editorUtilities.populateTemplate(valueTemplate, x),
                data: x.data,
              };
            });
          }

          return response;
        },
        formatResult: function (suggestion, currentValue) {
          const suggestionTemplate = searchField.getAttribute(
            "data-suggestion-template"
          );
          if (suggestionTemplate) {
            return editorUtilities.populateTemplate(
              suggestionTemplate,
              suggestion
            );
          }
          return $.Autocomplete.defaults.formatResult(suggestion, currentValue);
        },
      });
    }
  });
})();
