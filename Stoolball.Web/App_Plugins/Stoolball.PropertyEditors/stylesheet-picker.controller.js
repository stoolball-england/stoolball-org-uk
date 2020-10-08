angular
  .module("umbraco")
  .controller("Stoolball.StylesheetPickerController", function (
    $scope,
    stylesheetResource
  ) {
    stylesheetResource.getAll().then(function (stylesheets) {
      $scope.stylesheets = stylesheets.map((x) => x.name);
      if ($scope.model.config.filter) {
        const regex = RegExp($scope.model.config.filter);
        $scope.stylesheets = $scope.stylesheets.filter((x) => regex.test(x));
      }
    });
  });
