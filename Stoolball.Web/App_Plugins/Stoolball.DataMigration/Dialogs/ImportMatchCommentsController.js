(function () {
  "use strict";

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.ImportMatchComments", function (
      $http,
      $scope,
      umbRequestHelper,
      stoolballResource
    ) {
      let vm = this;

      vm.submit = submit;
      vm.close = close;
      vm.total = "?";
      vm.imported = [];
      vm.failed = [];
      vm.buttonState = "init";
      vm.processing = false;
      vm.done = false;

      async function getMatchCommentsToMigrate(dataSource, apiKey) {
        return await umbRequestHelper.resourcePromise(
          $http.get(
            "https://" + dataSource + "/data/comments-api.php?key=" + apiKey
          ),
          "Failed to retrieve all Stoolball England match comments data"
        );
      }
      async function importMatchComments(comments, imported, failed) {
        await stoolballResource.postManyToApi(
          "MatchCommentMigration/CreateMatchComment",
          comments,
          (comment) => ({
            MigratedMatchId: comment.matchId,
            MigratedMemberId: comment.user_id,
            MigratedMemberEmail: comment.email,
            CommentDate: comment.date_added,
            Comment: comment.message,
          }),
          imported,
          failed
        );
      }

      function submit() {
        vm.buttonState = "busy";

        stoolballResource.getApiKey().then(async (apiKey) => {
          vm.processing = true;
          try {
            let comments = await getMatchCommentsToMigrate(
              $scope.model.dataSource,
              apiKey
            );

            vm.total = comments.length;

            await importMatchComments(comments, vm.imported, vm.failed);

            vm.done = true;
            vm.buttonState = "success";
          } catch (e) {
            vm.buttonState = "error";
            console.log(e);
          }
        });
      }

      function close() {
        if ($scope.model.close) {
          $scope.model.close();
        }
      }
    });
})();
