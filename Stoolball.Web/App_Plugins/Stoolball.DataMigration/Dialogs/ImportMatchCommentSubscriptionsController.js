(function () {
  "use strict";

  angular
    .module("umbraco")
    .controller(
      "Stoolball.DataMigration.ImportMatchCommentSubscriptions",
      function ($http, $scope, umbRequestHelper, stoolballResource) {
        let vm = this;

        vm.submit = submit;
        vm.close = close;
        vm.total = "?";
        vm.imported = [];
        vm.failed = [];
        vm.buttonState = "init";
        vm.processing = false;
        vm.done = false;

        async function getMatchCommentSubscriptionsToMigrate(
          dataSource,
          apiKey
        ) {
          return await umbRequestHelper.resourcePromise(
            $http.get(
              "https://" +
                dataSource +
                "/data/subscriptions-api.php?key=" +
                apiKey
            ),
            "Failed to retrieve all Stoolball England match comment subscriptions data"
          );
        }
        async function importMatchCommentSubscriptions(
          subscriptions,
          imported,
          failed
        ) {
          await stoolballResource.postManyToApi(
            "MatchCommentSubscriptionMigration/CreateMatchCommentSubscription",
            subscriptions,
            (subscription) => ({
              MigratedMatchId: subscription.matchId,
              MigratedMemberId: subscription.user_id,
              MigratedMemberEmail: subscription.email,
              DisplayName: subscription.displayName,
              SubscriptionDate: subscription.date_changed,
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
              let subscriptions = await getMatchCommentSubscriptionsToMigrate(
                $scope.model.dataSource,
                apiKey
              );

              vm.total = subscriptions.length;

              await importMatchCommentSubscriptions(
                subscriptions,
                vm.imported,
                vm.failed
              );

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
      }
    );
})();
