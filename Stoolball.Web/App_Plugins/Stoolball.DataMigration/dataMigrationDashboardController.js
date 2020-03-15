(function () {
    "use strict";

    angular.module("umbraco").controller("Stoolball.DataMigration.DashboardController", function ($scope, editorService) {
        let vm = this;
        let dataSources = ["stoolball.local", "www.stoolball.org.uk"];

        vm.deleteMembers = function () {
            let dialogOptions = {
                title: "Are you sure?",
                size: "small",
                view: '/App_Plugins/Stoolball.DataMigration/dialogs/DeleteMembers.html',
                close: function () {
                    editorService.close();
                }
            };
            editorService.open(dialogOptions);
        }

        vm.deleteMemberGroups = function () {
            let dialogOptions = {
                title: "Are you sure?",
                size: "small",
                view: '/App_Plugins/Stoolball.DataMigration/dialogs/DeleteMemberGroups.html',
                close: function () {
                    editorService.close();
                }
            };
            editorService.open(dialogOptions);
        }

        vm.importMembers = function () {
            let dialogOptions = {
                title: "Import Member Groups and Members",
                size: "small",
                view: '/App_Plugins/Stoolball.DataMigration/dialogs/ImportGroupsAndMembers.html',
                close: function () {
                    editorService.close();
                },
                dataSources
            };
            editorService.open(dialogOptions);
        }
    }).controller("Stoolball.DataMigration.DeleteMembers", function ($scope, entityResource, stoolballResource) {

        let vm = this;

        vm.submit = submit;
        vm.close = close;
        vm.totalMembers = -1;
        vm.deletedMembers = [];
        vm.buttonState = "init";
        vm.done = false;

        function submit() {
            vm.buttonState = "busy";

            entityResource.getAll('member').then(async function (members) {
                vm.totalMembers = members.length;

                try {
                    await stoolballResource.deleteMembers(members, vm.deletedMembers);
                    vm.done = true;
                    vm.buttonState = "success";
                }
                catch (e) {
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

    }).controller("Stoolball.DataMigration.DeleteMemberGroups", function ($scope, stoolballResource) {

        let vm = this;

        vm.submit = submit;
        vm.close = close;
        vm.totalGroups = -1;
        vm.deletedGroups = [];
        vm.buttonState = "init";
        vm.done = false;

        function submit() {
            vm.buttonState = "busy";

            stoolballResource.getMemberGroups().then(async function (groups) {
                groups = groups.filter(group => group.Name !== "All Members");
                vm.totalGroups = groups.length;

                try {
                    await stoolballResource.deleteMemberGroups(groups, vm.deletedGroups);
                    vm.done = true;
                    vm.buttonState = "success";
                }
                catch (e) {
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

    }).controller("Stoolball.DataMigration.ImportGroupsAndMembers", function ($scope, stoolballResource) {

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
                                umbracoGroups.push({ id: group.roleId, name: scope.replace('https://www.stoolball.org.uk/id/', '') });
                            }
                        }
                    }
                    if (umbracoGroups.length === groupCountBeforeLoop) {
                        umbracoGroups.push({ id: group.roleId, name: group.name });
                    }
                };
                return umbracoGroups;
            }

            stoolballResource.getApiKey().then(async (apiKey) => {
                vm.processing = true;
                try {
                    let [groups, members] = await Promise.all([
                        stoolballResource.getMemberGroupsToMigrate($scope.model.dataSource, apiKey),
                        stoolballResource.getMembersToMigrate($scope.model.dataSource, apiKey)
                    ]);

                    groups = groups.filter(group => group.name !== "All Members" && group.name !== "Signed up user");
                    groups = transformGroups(groups);
                    vm.totalGroups = groups.length;
                    vm.totalMembers = members.length;

                    await stoolballResource.importMemberGroups(groups, vm.importedGroups);
                    await stoolballResource.importMembers(members, vm.importedMembers);
                    await stoolballResource.assignMembersToGroups(members, groups);

                    vm.done = true;
                    vm.buttonState = "success";
                }
                catch (e) {
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