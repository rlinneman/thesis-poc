(function () {
    'use strict';

    var componentId = 'assetMruService', s_mru = {};

    angular
        .module('app').factory(componentId, assetMruService)
        .controller('AssetMruController', function (job, assetMruService) {
            var vm = angular.extend(this, {
                mru: assetMruService.mru(job)
            });
        });

    function assetMruService(utils) {
        var service = {
            mru: function (job) { return (s_mru[job.id] || (s_mru[job.id] = [])); },
            push: function (asset, job) { return push(asset, job, service.mru(job)); }
        };

        return service;

        function push(asset, job, mru) {
            var known = utils.findById(mru, asset.id);
            if (known) {
                mru.splice(mru.indexOf(known), 1);
            }
            mru.unshift(asset);

            return asset;
        }
    }
})();