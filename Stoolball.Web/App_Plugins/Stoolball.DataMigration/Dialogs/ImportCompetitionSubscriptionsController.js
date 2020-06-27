(function () {
  "use strict";

  angular
    .module("umbraco")
    .controller(
      "Stoolball.DataMigration.ImportCompetitionSubscriptions",
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

        async function getCompetitionSubscriptionsToMigrate(
          dataSource,
          apiKey
        ) {
          return await umbRequestHelper.resourcePromise(
            $http.get(
              "https://" +
                dataSource +
                "/data/competitions-api.php?key=" +
                apiKey
            ),
            "Failed to retrieve all Stoolball England competition subscriptions data"
          );
        }
        async function importMatchCommentSubscriptions(
          subscriptions,
          imported,
          failed
        ) {
          await stoolballResource.postManyToApi(
            "CompetitionSubscriptionMigration/CreateCompetitionSubscription",
            subscriptions,
            (subscription) => ({
              MigratedCompetitionId: subscription.competitionId,
              MigratedMemberEmail: subscription.notificationEmail,
              DisplayName: "Matches in " + subscription.name,
              SubscriptionDate: subscription.dateCreated,
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
              let subscriptions = await getCompetitionSubscriptionsToMigrate(
                $scope.model.dataSource,
                apiKey
              );

              subscriptions = subscriptions.filter((x) => x.notificationEmail);
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
