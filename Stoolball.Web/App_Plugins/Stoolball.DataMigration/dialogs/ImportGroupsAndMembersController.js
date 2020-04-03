(function() {
  "use strict";

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.ImportGroupsAndMembers", function(
      $http,
      $scope,
      stoolballResource,
      umbRequestHelper
    ) {
      let vm = this;

      vm.submit = submit;
      vm.close = close;
      vm.totalMembers = "?";
      vm.totalGroups = "?";
      vm.importedGroups = [];
      vm.importedMembers = [];
      vm.buttonState = "init";
      vm.processing = false;
      vm.done = false;

      async function getMemberGroupsToMigrate(dataSource, apiKey) {
        const url =
          "https://" + dataSource + "/data/roles-api.php?key=" + apiKey;
        return await umbRequestHelper.resourcePromise(
          $http.get(url),
          "Failed to retrieve all Stoolball England role data"
        );
      }

      async function getMembersToMigrate(dataSource, apiKey) {
        const url =
          "https://" + dataSource + "/data/users-api.php?key=" + apiKey;
        return await umbRequestHelper.resourcePromise(
          $http.get(url),
          "Failed to retrieve all Stoolball England user data"
        );
      }

      async function importMemberGroups(groups, imported, failed) {
        await stoolballResource.postManyToApi(
          "MemberMigration/CreateMemberGroup",
          groups,
          group => ({
            Name: group.name
          }),
          imported,
          failed
        );
      }

      async function importMembers(members, imported) {
        await stoolballResource.postManyToApi(
          "MemberMigration/CreateMember",
          members,
          member => member,
          imported
        );
      }

      async function assignMembersToGroups(members, groups) {
        await stoolballResource.asyncForEach(members, async member => {
          for (let i = 0; i < member.roles.length; i++) {
            let roleId = member.roles[i];
            let groupsToAssign = groups.filter(group => group.id == roleId);
            await stoolballResource.postManyToApi(
              "MemberMigration/AssignMemberGroup",
              groupsToAssign,
              group => ({
                Email: member.email,
                GroupName: group.name
              })
            );
          }
        });
      }

      function submit() {
        vm.buttonState = "busy";

        function transformGroups(groups) {
          let umbracoGroups = [];
          for (let i = 0; i < groups.length; i++) {
            // Each migrated group might have multiple permissions that now need to be separate groups.
            // Ignore "global" permissions, but include the resource URI for others.
            let group = groups[i];
            let groupCountBeforeLoop = umbracoGroups.length;
            for (let i = 0; i < group.permissions.length; i++) {
              for (let j = 0; j < group.permissions[i].scopes.length; j++) {
                let scope = group.permissions[i].scopes[j];
                if (scope !== "global") {
                  umbracoGroups.push({
                    id: group.roleId,
                    name: scope.replace("https://www.stoolball.org.uk/id/", "")
                  });
                }
              }
            }
            if (umbracoGroups.length === groupCountBeforeLoop) {
              umbracoGroups.push({ id: group.roleId, name: group.name });
            }
          }
          return umbracoGroups;
        }

        stoolballResource.getApiKey().then(async apiKey => {
          vm.processing = true;
          try {
            let [groups, members] = await Promise.all([
              getMemberGroupsToMigrate($scope.model.dataSource, apiKey),
              getMembersToMigrate($scope.model.dataSource, apiKey)
            ]);

            groups = groups.filter(
              group =>
                group.name !== "All Members" && group.name !== "Signed up user"
            );
            groups = transformGroups(groups);
            vm.totalGroups = groups.length;
            vm.totalMembers = members.length;

            await importMemberGroups(groups, vm.importedGroups);
            await importMembers(members, vm.importedMembers);
            await assignMembersToGroups(members, groups);

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
