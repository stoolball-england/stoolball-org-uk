function stoolballResource($http, umbRequestHelper, memberResource) {
  // the factory object returned
  return {
    __asyncForEach: async (array, callback) => {
      for (let index = 0; index < array.length; index++) {
        await callback(array[index], index, array);
      }
    },
    __parseXsrfTokenFromCookie(cookie) {
      const split = cookie.split(";");
      const xsrf = split.find(
        value => value.replace(/\s/, "").indexOf("UMB-XSRF-TOKEN") === 0
      );
      return xsrf ? xsrf.split("=")[1] : null;
    },
    async __postManyToApi(apiRoute, items, itemReducer, succeeded, failed) {
      await this.__asyncForEach(items, async item => {
        await this.__postToApi(apiRoute, item, itemReducer, succeeded, failed);
      });
    },
    async __postToApi(apiRoute, item, itemReducer, succeeded, failed) {
      await fetch("/umbraco/backoffice/Migration/" + apiRoute, {
        method: "POST",
        credentials: "same-origin",
        headers: {
          "Content-Type": "application/json",
          "X-Umb-XSRF-Token": this.__parseXsrfTokenFromCookie(document.cookie)
        },
        body: itemReducer ? JSON.stringify(itemReducer(item)) : null
      }).then(response => {
        if (response.ok && succeeded) {
          succeeded.push(item);
        }
        if (!response.ok && failed) {
          failed.push(item);
        }
      });
    },
    async __deleteApi(apiRoute, callback) {
      await fetch("/umbraco/backoffice/Migration/" + apiRoute, {
        method: "DELETE",
        credentials: "same-origin",
        headers: {
          "Content-Type": "application/json",
          "X-Umb-XSRF-Token": this.__parseXsrfTokenFromCookie(document.cookie)
        }
      });
    },
    // this calls the Stoolball England data migration API
    getApiKey: function() {
      const url = "/umbraco/backoffice/Migration/MemberMigration/ApiKey";
      return umbRequestHelper.resourcePromise(
        $http.get(url),
        "Failed to retrieve API key"
      );
    },
    getMemberGroupsToMigrate: async function(dataSource, apiKey) {
      const url = "https://" + dataSource + "/data/roles-api.php?key=" + apiKey;
      return await umbRequestHelper.resourcePromise(
        $http.get(url),
        "Failed to retrieve all Stoolball England role data"
      );
    },
    getMembersToMigrate: async function(dataSource, apiKey) {
      const url = "https://" + dataSource + "/data/users-api.php?key=" + apiKey;
      return await umbRequestHelper.resourcePromise(
        $http.get(url),
        "Failed to retrieve all Stoolball England user data"
      );
    },
    deleteMembers: async function(members, deleted) {
      await this.__asyncForEach(members, async member => {
        await memberResource.deleteByKey(member.key);
        if (deleted) {
          deleted.push(member);
        }
      });
    },
    getMemberGroups: function() {
      const url = "/umbraco/backoffice/Migration/MemberMigration/MemberGroups";
      return umbRequestHelper.resourcePromise(
        $http.get(url),
        "Failed to retrieve member groups"
      );
    },
    deleteMemberGroups: async function(groups, deleted) {
      await this.__postManyToApi(
        "MemberMigration/DeleteMemberGroup",
        groups,
        group => group,
        deleted
      );
    },
    importMembers: async function(members, imported) {
      await this.__postManyToApi(
        "MemberMigration/CreateMember",
        members,
        member => member,
        imported
      );
    },
    importMemberGroups: async function(groups, imported, failed) {
      await this.__postManyToApi(
        "MemberMigration/CreateMemberGroup",
        groups,
        group => ({
          Name: group.name
        }),
        imported,
        failed
      );
    },
    assignMembersToGroups: async function(members, groups) {
      await this.__asyncForEach(members, async member => {
        for (let i = 0; i < member.roles.length; i++) {
          let roleId = member.roles[i];
          let groupsToAssign = groups.filter(group => group.id == roleId);
          await this.__postManyToApi(
            "MemberMigration/AssignMemberGroup",
            groupsToAssign,
            group => ({
              Email: member.email,
              GroupName: group.name
            })
          );
        }
      });
    },
    ensureRedirects: async function() {
      await this.__postToApi("RedirectsMigration/EnsureRedirects");
    },
    getClubsToMigrate: async function(dataSource, apiKey) {
      const url = "https://" + dataSource + "/data/clubs-api.php?key=" + apiKey;
      return await umbRequestHelper.resourcePromise(
        $http.get(url),
        "Failed to retrieve all Stoolball England club data"
      );
    },
    importClubs: async function(clubs, imported, failed) {
      await this.__postManyToApi(
        "ClubMigration/CreateClub",
        clubs,
        club => ({
          ClubId: club.clubId,
          ClubName: club.name,
          PlaysOutdoors: club.playsOutdoors,
          PlaysIndoors: club.playsIndoors,
          Twitter: club.twitterAccount,
          Facebook: club.facebookUrl,
          Instagram: club.instagramAccount,
          ClubMark: club.clubmarkAccredited,
          HowManyPlayers: club.howManyPlayers,
          ClubRoute: club.route,
          DateCreated: club.dateCreated,
          DateUpdated: club.dateUpdated
        }),
        imported,
        failed
      );
    },
    deleteClubs: async function() {
      return await this.__deleteApi("ClubMigration/DeleteClubs");
    },
    getSchoolsToMigrate: async function(dataSource, apiKey) {
      const url =
        "https://" +
        dataSource +
        "/data/clubs-api.php?type=schools&key=" +
        apiKey;
      return await umbRequestHelper.resourcePromise(
        $http.get(url),
        "Failed to retrieve all Stoolball England schools data"
      );
    },
    importSchools: async function(schools, imported, failed) {
      await this.__postManyToApi(
        "SchoolMigration/CreateSchool",
        schools,
        school => ({
          SchoolId: school.clubId,
          SchoolName: school.name,
          PlaysOutdoors: school.playsOutdoors,
          PlaysIndoors: school.playsIndoors,
          Twitter: school.twitterAccount,
          Facebook: school.facebookUrl,
          Instagram: school.instagramAccount,
          HowManyPlayers: school.howManyPlayers,
          SchoolRoute: school.route,
          DateCreated: school.dateCreated,
          DateUpdated: school.dateUpdated
        }),
        imported,
        failed
      );
    },
    deleteSchools: async function() {
      return await this.__deleteApi("SchoolMigration/DeleteSchools");
    }
  };
}

// For Jest tests
if (typeof module !== "undefined" && typeof module.exports !== "undefined") {
  module.exports = stoolballResource;
}

//adds the resource to umbraco.resources module:
if (typeof angular !== "undefined") {
  angular
    .module("umbraco.resources")
    .factory("stoolballResource", stoolballResource);
}
