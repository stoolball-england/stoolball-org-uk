(function () {
  "use strict";

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.ImportCompetitions", function (
      $http,
      $scope,
      stoolballResource,
      umbRequestHelper
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

      async function getCompetitionsToMigrate(dataSource, apiKey) {
        const url =
          "https://" + dataSource + "/data/competitions-api.php?key=" + apiKey;
        return await umbRequestHelper.resourcePromise(
          $http.get(url),
          "Failed to retrieve all Stoolball England competition data"
        );
      }

      async function importCompetitions(competitions, imported, failed) {
        await stoolballResource.postManyToApi(
          "CompetitionMigration/CreateCompetition",
          competitions,
          (competition) => ({
            CompetitionId: competition.competitionId,
            CompetitionName: competition.name,
            PlayerType: competition.playerType,
            Introduction: competition.introduction,
            UntilDate: competition.untilDate,
            Website: competition.website,
            Twitter: competition.twitterAccount,
            Facebook: competition.facebookUrl,
            Instagram: competition.instagramAccount,
            PublicContactDetails: competition.publicContactDetails,
            PlayersPerTeam: competition.playersPerTeam,
            Overs: competition.overs,
            CompetitionRoute: competition.route,
            History: [
              {
                Action: "Create",
                AuditDate: competition.dateCreated,
              },
              {
                Action: "Update",
                AuditDate: competition.dateUpdated,
              },
            ],
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
            let competitions = await getCompetitionsToMigrate(
              $scope.model.dataSource,
              apiKey
            );

            vm.total = competitions.length;

            await importCompetitions(competitions, vm.imported, vm.failed);

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
