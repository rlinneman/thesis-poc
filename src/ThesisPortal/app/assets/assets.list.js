(function () {
    'use strict';

    var componentId = 'AssetsListController', s_mru = {};

    angular.module('app').controller('AssetsListController', AssetsListController);

    function AssetsListController(job, $state, $scope, jobService, toastr) {
        var vm = angular.extend(this, {
            assetAreas: [],
            jobId: job.id,

            haveSearched: false,
            noAssetsAssigned: false,

            chosenArea: null
        });

        $scope.$watch('vm.chosenArea', findAssets);

        jobService
            .getAssetAreas(job)
            .then(function (_) {
                vm.assetAreas = _;
                if (!(_ && _.length)) {
                    // event empty string asset area will be returned, if there
                    // are no areas found, there are no assets to search on.
                    vm.noAssetsAssigned = true;
                }
            }, function (e) {
                returnToJob('Error while attempting to load asset areas.');
            });

        function findAssets() {
            if (!!vm.chosenArea) {
                jobService.getAssets(job, vm.chosenArea)
                    .then(function (assets) {
                        vm.haveSearched = true;
                        vm.assets = assets;
                        if (!assets) {
                            vm.noAssetsAssigned = true;
                        }
                    }, function (e) {
                        returnToJob('Unable to get assets');
                    });
            }
        }

        function returnToJob(reason) {
            toastr.error(reason, 'Assets');
            $state.go('job.details');
        }
    }
})();