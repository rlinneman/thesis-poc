(function () {
    'use strict';

    angular.module('app').config(function ($stateProvider, $urlRouterProvider) {
        $urlRouterProvider.otherwise('/jobs');

        $stateProvider.state('job.assets', {
            url: '/assets',
            abstract: true,
            views: {
                'sidebar@job': {
                    templateUrl: '/app/assets/assets.sidebar.html',
                    controllerAs: 'vma',
                    controller: 'AssetMruController'
                },
                '': { template: '<ui-view/>' }
            },

            resolve: {
                identity: function (principal) { return principal.identity(); },
                //mru: function ($q) { return $q.when([{ id: 123, prefix: 'AHU', rank: 9, area: 'basement' }]); }
            },
        }).state('job.assets.list', {
            url: '',
            views: {
                '': {
                    templateUrl: '/app/assets/assets.list.html',
                    controller: 'AssetsListController',
                    controllerAs: 'vm',
                },
            },
        })




        .state('job.asset', {
            url: '/assets/{assetId:[0-9]+}',
            abstract: true,
            views: {
                'sidebar@job': {
                    templateUrl: '/app/assets/assets.sidebar.html',
                    controllerAs: 'vma',
                    controller: 'AssetMruController'
                },
                '': { template: '<ui-view/>' }
            },

            resolve: {
                identity: function (principal) { return principal.identity(); },
                asset: function (job, $q, $state, $stateParams, jobService, utils, assetMruService) {
                    var assetId = utils.coerceToInt($stateParams.assetId);
                    if (!assetId) { return $q.reject('Asset id is required.'); }

                    if ($state.asset && $state.asset.id === assetId) {
                        assetMruService.push($state.asset, job);
                        return $q.when($state.asset);
                    }

                    return jobService.getAssetByJobAndId(job, assetId).then(function (asset) {
                        if (!asset) { return $q.reject('asset not found'); }
                        assetMruService.push(asset, job);
                        return ($state.asset = asset);
                    });
                }
            },
        })
        .state('job.asset.details', {
            url: '',
            templateUrl: '/app/assets/asset.details.html',
            controllerAs: 'vm',
            controller: 'AssetDetailsController'
        });
    });

    
})();