(function () {
    'use strict';

    angular.module('app').config(function ($stateProvider, $urlRouterProvider) {


        $stateProvider
            .state('assets', {
                abstract: true,
                url: '/{jobId:[0-9]+}/assets',
                templateUrl: '/app/assets/assets.html',
                controllerAs: 'vm',
                controller: function (areas, mru, assetsService, jobsService, $log, $scope, $stateParams) {
                    var vm = angular.extend(this, {
                        job: null,
                        assets: [],
                        areas: areas,
                        mru: mru,
                        offline: null,
                        name:assetsService.name
                    });

                    
                    jobsService.byId($stateParams.jobId).then(function (job) {
                        vm.job = job;
                        vm.offline = job.offline;
                    });

                    $scope.$watch('vm.assetType', findAssets);
                    $scope.$watch('vm.assetArea', findAssets);

                    function findAssets() {
                        if (!!vm.assetType || !!vm.assetArea) {
                            assetsService.byJobTypeArea(vm.job, vm.assetType, vm.assetArea).then(function (assets) {
                                vm.assets = assets;
                            }, function (e) {
                                $log.error('Unable to get assets', e);
                            });
                        }
                    }
                },
                resolve: {
                    identity: function (principal) {
                        return principal.identity();
                    },
                    areas: function (assetsService, $stateParams) {
                        return assetsService.allAreas($stateParams.jobId);
                    },
                    mru: function () { return [];}
                    //assets: function ($stateParams, assetsService) {
                    //    return assetsService.byJobIdAndParent($stateParams.jobId);
                    //},
                }
            })
        .state('assets.list', {
            url: '',
            views: {
                'filter': {
                    templateUrl: '/app/assets/assets.filter.html',
                },
                'list': {
                    templateUrl: '/app/assets/assets.list.html',
                    
                }
            }
        })
        .state('assets.detail', {
            url: '/{assetId:[0-9]+}',
            templateUrl: '/app/assets/assets.detail.html',
            controller: function (mru, utils, assetsService, $stateParams) {
                var vm = angular.extend(this, {
                    name: assetsService.name,
                    asset:null
                });
                
                activate($stateParams.assetId, $stateParams.jobId);

                // check for recently used local version
                function activate(assetId, jobId) {
                    var asset = utils.findById(mru, assetId);
                    if (asset) {
                        var i = mru.indexOf(asset);
                        mru.splice(i, 1);
                        mru.unshift(asset);

                        return angular.extend(vm, asset, { asset: asset });
                    }
                    
                    return assetsService
                        .byJobAndId(jobId, assetId)
                        .then(function (asset) {
                            mru.unshift(asset);
                            angular.extend(vm, asset, { asset: asset });
                        });
                }
            }
        });
    });

})();