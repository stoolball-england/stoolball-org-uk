function stoolballResource($http, umbRequestHelper) {
  // the factory object returned
  return {
    async asyncForEach(array, callback) {
      if (!array || !array.length || !callback) return;
      for (let index = 0; index < array.length; index++) {
        await callback(array[index], index, array);
      }
    },
    parseXsrfTokenFromCookie(cookie) {
      const split = cookie.split(";");
      const xsrf = split.find(
        (value) => value.replace(/\s/, "").indexOf("UMB-XSRF-TOKEN") === 0
      );
      return xsrf ? xsrf.split("=")[1] : null;
    },
    async postManyToApi(apiRoute, items, itemReducer, succeeded, failed) {
      if (!items || !items.length) return;
      const collectionSucceeded = [],
        collectionFailed = [];
      await this.postToApi(
        apiRoute,
        items.map((x) => itemReducer(x)),
        null,
        collectionSucceeded,
        collectionFailed
      );
      if (collectionSucceeded.length && succeeded) {
        succeeded.push(...collectionSucceeded[0]);
      }
      if (collectionFailed.length && failed) {
        failed.push(...collectionFailed[0]);
      }
    },
    async postToApi(apiRoute, item, itemReducer, succeeded, failed) {
      await fetch("/umbraco/backoffice/Migration/" + apiRoute, {
        method: "POST",
        credentials: "same-origin",
        headers: {
          "Content-Type": "application/json",
          "X-Umb-XSRF-Token": this.parseXsrfTokenFromCookie(document.cookie),
        },
        body: itemReducer
          ? JSON.stringify(itemReducer(item))
          : JSON.stringify(item),
      }).then((response) => {
        if (response.ok && succeeded) {
          succeeded.push(item);
        }
        if (!response.ok && failed) {
          failed.push(item);
        }
      });
    },
    async deleteApi(apiRoute, callback) {
      await fetch("/umbraco/backoffice/Migration/" + apiRoute, {
        method: "DELETE",
        credentials: "same-origin",
        headers: {
          "Content-Type": "application/json",
          "X-Umb-XSRF-Token": this.parseXsrfTokenFromCookie(document.cookie),
        },
      }).then((response) => callback(response.ok));
    },
    // this calls the Stoolball England data migration API
    getApiKey() {
      const url = "/umbraco/backoffice/Migration/MemberMigration/ApiKey";
      return umbRequestHelper.resourcePromise(
        $http.get(url),
        "Failed to retrieve API key"
      );
    },
  };
}

// For Jest tests
if (typeof module !== "undefined" && typeof module.exports !== "undefined") {
  module.exports = stoolballResource;
}

//adds the resource to umbraco.resources module:
if (typeof angular !== "undefined") {
  angular
    .module("umbraco.resources")
    .factory("stoolballResource", stoolballResource);
}
