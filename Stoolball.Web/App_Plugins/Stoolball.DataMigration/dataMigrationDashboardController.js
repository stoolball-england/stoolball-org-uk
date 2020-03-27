(function() {
  "use strict";

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.DashboardController", function(
      $scope,
      editorService
    ) {
      let vm = this;

      vm.openDialog = function(title, filename) {
        editorService.open({
          size: "small",
          title,
          view:
            "/App_Plugins/Stoolball.DataMigration/dialogs/" +
            filename +
            ".html",
          close: function() {
            editorService.close();
          },
          dataSources: ["stoolball.local", "www.stoolball.org.uk"]
        });
      };
    });
})();
