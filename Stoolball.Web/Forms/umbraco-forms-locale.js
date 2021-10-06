// deliberately using var to be in global scope and running immediately - out-of-the-box this is inline script
const locale = document.getElementById("umbraco-forms-locale");
if (locale) {
  var umbracoFormsLocale = JSON.parse(locale.getAttribute("data-locale"));
}
