angular.module("umbraco").controller("TipElementTypeController", function ($scope) {
    $scope.block.data.text.markup = $scope.block.data.text.markup.replace(/<(?!\/?(strong|a|em)(?=>|\s?.*>))\/?.*?>/gi, '');
});