angular.module("umbraco").controller("GettyImageController", function ($scope, $sce) {
    $scope.trustSrc = function (src) {
        return $sce.trustAsResourceUrl(src);
    }
});