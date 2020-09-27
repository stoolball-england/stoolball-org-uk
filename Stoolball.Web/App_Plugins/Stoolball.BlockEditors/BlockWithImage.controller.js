angular.module("umbraco").controller("BlockWithImageController", function ($scope, mediaResource) {

    const imageUdi = $scope.block.data.image;
    if (imageUdi) {
        mediaResource.getById(imageUdi)
            .then(function (media) {
                $scope.image = media;
            });
    }

    $scope.imagePosition = ($scope.block.data.imagePosition && $scope.block.data.imagePosition.toLowerCase()) || "left";
});