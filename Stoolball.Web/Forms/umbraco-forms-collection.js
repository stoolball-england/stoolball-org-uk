// deliberately using var to be in global scope and running immediately - out-of-the-box this is inline script
const umbracoFormsCollectionData = document.getElementById(
  "umbraco-forms-collection"
);
if (umbracoFormsCollectionData) {
  if (typeof umbracoFormsCollection === "undefined") {
    var umbracoFormsCollection = [];
  }
  umbracoFormsCollection.push(
    umbracoFormsCollectionData.getAttribute("data-forms")
  );
}
