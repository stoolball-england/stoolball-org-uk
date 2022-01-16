// deliberately using var to be in global scope and running immediately - out-of-the-box this is inline script
if (typeof umbracoFormsCollection === "undefined") {
  var umbracoFormsCollection = [];
}
umbracoFormsCollection.push(
  document.getElementById("umbraco-forms-collection").getAttribute("data-forms")
);
