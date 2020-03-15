// adds the resource to umbraco.resources module:
angular.module('umbraco.resources').factory('stoolballResource',
    function ($http, umbRequestHelper, memberResource) {

        // the factory object returned
        return {
            __asyncForEach: async (array, callback) => {
                for (let index = 0; index < array.length; index++) {
                    await callback(array[index], index, array);
                }
            },

            // this calls the Stoolball England data migration API
            getApiKey: function () {
                const url = "/umbraco/backoffice/Migration/MemberMigration/ApiKey";
                return umbRequestHelper.resourcePromise(
                    $http.get(url),
                    "Failed to retrieve API key");
            },
            getMemberGroupsToMigrate: async function (dataSource, apiKey) {
                const url = "https://" + dataSource + "/data/roles-api.php?key=" + apiKey;
                return await umbRequestHelper.resourcePromise(
                    $http.get(url),
                    "Failed to retrieve all Stoolball England role data");
            },
            getMembersToMigrate: async function (dataSource, apiKey) {
                const url = "https://" + dataSource + "/data/users-api.php?key=" + apiKey;
                return await umbRequestHelper.resourcePromise(
                    $http.get(url),
                    "Failed to retrieve all Stoolball England user data");
            },
            deleteMembers: async function (members, deleted) {
                await this.__asyncForEach(members, async (member) => {
                    await memberResource.deleteByKey(member.key);
                    if (deleted) { deleted.push(member); }
                });
            },
            getMemberGroups: function () {
                const url = "/umbraco/backoffice/Migration/MemberMigration/MemberGroups";
                return umbRequestHelper.resourcePromise(
                    $http.get(url),
                    "Failed to retrieve member groups");
            },
            deleteMemberGroups: async function (groups, deleted) {
                await this.__asyncForEach(groups, async (groupToDelete) => {
                    await fetch("/umbraco/backoffice/Migration/MemberMigration/DeleteMemberGroup",
                        {
                            method: "POST",
                            credentials: 'same-origin',
                            headers: {
                                'Content-Type': 'application/json',
                                'X-Umb-XSRF-Token': document.cookie.split('=')[1]
                            },
                            body: JSON.stringify(groupToDelete)
                        });
                    if (deleted) { deleted.push(groupToDelete); }
                });
            },
            importMembers: async function (members, imported) {
                await this.__asyncForEach(members, async (memberToImport) => {
                    await fetch("/umbraco/backoffice/Migration/MemberMigration/CreateMember",
                        {
                            method: "POST",
                            credentials: 'same-origin',
                            headers: {
                                'Content-Type': 'application/json',
                                'X-Umb-XSRF-Token': document.cookie.split('=')[1]
                            },
                            body: JSON.stringify(memberToImport)
                        });
                    if (imported) { imported.push(memberToImport); }
                });
            },
            importMemberGroups: async function (groups, imported) {
                await this.__asyncForEach(groups, async (groupToImport) => {
                    await fetch("/umbraco/backoffice/Migration/MemberMigration/CreateMemberGroup",
                        {
                            method: "POST",
                            credentials: 'same-origin',
                            headers: {
                                'Content-Type': 'application/json',
                                'X-Umb-XSRF-Token': document.cookie.split('=')[1]
                            },
                            body: JSON.stringify({ Name: groupToImport.name })
                        });
                    if (imported) { imported.push(groupToImport); }
                });
            },
            assignMembersToGroups: async function (members, groups) {
                await this.__asyncForEach(members, async (member) => {
                    for (let i = 0; i < member.roles.length; i++) {
                        let roleId = member.roles[i];
                        let groupsToAssign = groups.filter(group => group.id == roleId);
                        for (let j = 0; j < groupsToAssign.length; j++) {
                            await fetch("/umbraco/backoffice/Migration/MemberMigration/AssignMemberGroup",
                                {
                                    method: "POST",
                                    credentials: 'same-origin',
                                    headers: {
                                        'Content-Type': 'application/json',
                                        'X-Umb-XSRF-Token': document.cookie.split('=')[1]
                                    },
                                    body: JSON.stringify({ Email: member.email, GroupName: groupsToAssign[j].name })
                                });
                        }
                    }
                })
            }
        };
    }
);