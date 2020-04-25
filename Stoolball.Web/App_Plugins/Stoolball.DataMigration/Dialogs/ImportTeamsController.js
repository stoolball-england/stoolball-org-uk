(function () {
  "use strict";

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.ImportTeams", function (
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

      async function getTeamsToMigrate(dataSource, apiKey) {
        const url =
          "https://" + dataSource + "/data/teams-api.php?key=" + apiKey;
        return await umbRequestHelper.resourcePromise(
          $http.get(url),
          "Failed to retrieve all Stoolball England team data"
        );
      }

      async function importTeams(teams, imported, failed) {
        await stoolballResource.postManyToApi(
          "TeamMigration/CreateTeam",
          teams,
          (team) => ({
            TeamId: team.teamId,
            TeamName: team.name,
            Club: { ClubId: team.clubId },
            School: { SchoolId: team.schoolId },
            MatchLocations: team.groundId
              ? [{ MatchLocationId: team.groundId }]
              : [],
            TeamType: team.teamType,
            PlayerType: team.playerType,
            Introduction: team.introduction,
            AgeRangeLower: team.ageRangeLower,
            AgeRangeUpper: team.ageRangeUpper,
            UntilDate: team.untilDate,
            Website: team.website,
            Twitter: team.twitterAccount,
            Facebook: team.facebookUrl,
            Instagram: team.instagramAccount,
            PublicContactDetails: team.publicContactDetails,
            PrivateContactDetails: team.privateContactDetails,
            PlayingTimes: team.playingTimes,
            Cost: team.cost,
            TeamRoute: team.route,
            History: [
              {
                Action: "Create",
                AuditDate: team.dateCreated,
              },
              {
                Action: "Update",
                AuditDate: team.dateUpdated,
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
            let teams = await getTeamsToMigrate(
              $scope.model.dataSource,
              apiKey
            );

            vm.total = teams.length;

            await importTeams(teams, vm.imported, vm.failed);

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
