(function () {
  "use strict";

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.ImportMatches", function (
      $http,
      $scope,
      umbRequestHelper,
      stoolballResource
    ) {
      let vm = this;

      vm.submit = submit;
      vm.close = close;
      vm.buttonState = "init";
      vm.processing = false;
      vm.done = false;

      async function getMatchesToMigrate(
        dataSource,
        apiKey,
        matchType,
        position,
        batchSize
      ) {
        return await umbRequestHelper.resourcePromise(
          $http.get(
            "https://" +
              dataSource +
              "/data/matches-api.php?key=" +
              apiKey +
              "&type=" +
              matchType +
              "&from=" +
              position +
              "&batchSize=" +
              batchSize
          ),
          "Failed to retrieve Stoolball England match data"
        );
      }

      async function importMatches(matches, imported, failed) {
        await stoolballResource.postManyToApi(
          "MatchMigration/CreateMatch",
          matches,
          (match) => {
            const teams = [];
            const home = match.teams.filter((team) => team.teamRole === 1);
            if (home.length) {
              teams.push({
                Team: { TeamId: home[0].teamId },
                TeamRole: 0,
                WonToss: match.tossWonBy ? match.tossWonBy === 1 : null,
              });
            }
            const away = match.teams.filter((team) => team.teamRole === 2);
            if (away.length) {
              teams.push({
                Team: { TeamId: away[0].teamId },
                TeamRole: 1,
                WonToss: match.tossWonBy ? match.tossWonBy === 2 : null,
              });
            }

            return {
              MatchId: match.matchId,
              MatchName: match.title,
              UpdateMatchNameAutomatically: !match.customTitle,
              MatchLocation: match.groundId
                ? { MatchLocationId: match.groundId }
                : null,
              MatchType: match.matchType,
              PlayerType: match.playerType - 1,
              PlayersPerTeam: match.playersPerTeam,
              MatchInnings: [
                {
                  Team: home.length ? { TeamId: home[0].teamId } : null,
                  InningsOrderInMatch: match.homeBatFirst
                    ? match.homeBatFirst
                      ? 1
                      : 2
                    : 1,
                  Overs: match.overs,
                  Runs: match.homeRuns,
                  Wickets: match.homeWickets,
                },
                {
                  Team: away.length ? { TeamId: away[0].teamId } : null,
                  InningsOrderInMatch: match.homeBatFirst
                    ? match.homeBatFirst
                      ? 2
                      : 1
                    : 2,
                  Overs: match.overs,
                  Runs: match.awayRuns,
                  Wickets: match.awayWickets,
                },
              ],
              InningsOrderIsKnown: match.homeBatFirst === null,
              OversPerInningsDefault: match.overs,
              Tournament: match.tournamentMatchId
                ? { TournamentId: match.tournamentMatchId }
                : null,
              OrderInTournament: match.orderInTournament,
              StartTime: match.startTime,
              StartTimeIsKnown: match.startTimeKnown,
              Teams: teams,
              MatchRoute: match.route,
              History: [
                {
                  AuditDate: match.dateCreated,
                  ActorName: match.createdBy ? match.createdBy : null,
                },
                {
                  AuditDate: match.dateUpdated,
                  ActorName: match.updatedBy ? match.updatedBy : null,
                },
              ],
              MatchResultType: match.resultType ? match.resultType - 1 : null,
              MatchNotes: match.notes,
            };
          },
          imported,
          failed
        );
      }

      async function importTournaments(tournaments, imported, failed) {
        await stoolballResource.postManyToApi(
          "MatchMigration/CreateTournament",
          tournaments,
          (tournament) => ({
            TournamentId: tournament.matchId,
            TournamentName: tournament.title,
            TournamentLocation: tournament.groundId
              ? { MatchLocationId: tournament.groundId }
              : null,
            TournamentQualificationType:
              tournament.qualification === 0
                ? null
                : tournament.qualification - 1,
            PlayerType: tournament.playerType - 1,
            PlayersPerTeam: tournament.playersPerTeam,
            OversPerInningsDefault: tournament.overs,
            MaximumTeamsInTournament: tournament.maximumTeamsInTournament,
            SpacesInTournament: tournament.spacesInTournament,
            StartTime: tournament.startTime,
            StartTimeIsKnown: tournament.startTimeKnown,
            MatchNotes: tournament.notes,
            Teams: tournament.teams.map((team) => ({
              Team: { TeamId: team.teamId },
              TeamRole: team.teamRole - 1,
            })),
            TournamentRoute: tournament.route,
            History: [
              {
                AuditDate: tournament.dateCreated,
                ActorName: tournament.createdBy ? tournament.createdBy : null,
              },
              {
                AuditDate: tournament.dateUpdated,
                ActorName: tournament.updatedBy ? tournament.updatedBy : null,
              },
            ],
          }),
          imported,
          failed
        );
      }

      vm.matchTypes = [
        {
          id: 0,
          name: "League matches",
          importMatches,
          imported: [],
          failed: [],
          total: "?",
        },
        {
          id: 1,
          name: "Tournaments",
          importMatches: importTournaments,
          imported: [],
          failed: [],
          total: "?",
        },
        {
          id: 2,
          name: "Tournament matches",
          importMatches,
          imported: [],
          failed: [],
          total: "?",
        },
        {
          id: 3,
          name: "Practices",
          importMatches,
          imported: [],
          failed: [],
          total: "?",
        },
        {
          id: 4,
          name: "Friendlies",
          importMatches,
          imported: [],
          failed: [],
          total: "?",
        },
        {
          id: 5,
          name: "Cup matches",
          importMatches,
          imported: [],
          failed: [],
          total: "?",
        },
      ];

      function submit() {
        vm.buttonState = "busy";

        stoolballResource.getApiKey().then(async (apiKey) => {
          vm.processing = true;
          try {
            const batchSize = 50;
            for (let i = 0; i < vm.matchTypes.length; i++) {
              let position = 0;
              let first = true;
              while (first || position <= vm.matchTypes[i].total) {
                first = false;

                let matches = await getMatchesToMigrate(
                  $scope.model.dataSource,
                  apiKey,
                  vm.matchTypes[i].id,
                  position,
                  batchSize
                );

                vm.matchTypes[i].total = matches.total;

                if (matches.matches && matches.matches.length) {
                  await vm.matchTypes[i].importMatches(
                    matches.matches,
                    vm.matchTypes[i].imported,
                    vm.matchTypes[i].failed
                  );
                }

                position = position + batchSize;
              }
            }
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