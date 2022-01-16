angular.module("umbraco").controller("TipElementTypeController", function ($scope) {
    $scope.block.data.text = $scope.block.data.text.replace(/<(?!\/?(strong|a|em)(?=>|\s?.*>))\/?.*?>/gi, '');
});