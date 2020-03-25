(function() {
  "use strict";

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.DashboardController", function(
      $scope,
      editorService
    ) {
      let vm = this;
      let dataSources = ["stoolball.local", "www.stoolball.org.uk"];

      vm.deleteMembers = function() {
        let dialogOptions = {
          title: "Are you sure?",
          size: "small",
          view:
            "/App_Plugins/Stoolball.DataMigration/dialogs/DeleteMembers.html",
          close: function() {
            editorService.close();
          }
        };
        editorService.open(dialogOptions);
      };

      vm.deleteMemberGroups = function() {
        let dialogOptions = {
          title: "Are you sure?",
          size: "small",
          view:
            "/App_Plugins/Stoolball.DataMigration/dialogs/DeleteMemberGroups.html",
          close: function() {
            editorService.close();
          }
        };
        editorService.open(dialogOptions);
      };

      vm.importMembers = function() {
        let dialogOptions = {
          title: "Import Member Groups and Members",
          size: "small",
          view:
            "/App_Plugins/Stoolball.DataMigration/dialogs/ImportGroupsAndMembers.html",
          close: function() {
            editorService.close();
          },
          dataSources
        };
        editorService.open(dialogOptions);
      };
    });
})();
