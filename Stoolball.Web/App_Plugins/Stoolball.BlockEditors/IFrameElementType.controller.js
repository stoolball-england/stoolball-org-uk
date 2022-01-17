angular.module("umbraco").controller("IFrameElementTypeController", function ($scope, $sce) {
    $scope.trustSrc = function (src) {
        return $sce.trustAsResourceUrl(src);
    }
});