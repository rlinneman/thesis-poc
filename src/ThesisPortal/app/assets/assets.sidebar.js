(function () {
    'use strict';

    var componentId = 'AssetSidebarController';

    angular.module('app').controller(componentId, AssetSidebarController);




    function AssetSidebarController($stateParams, $q, jobService, $log) {
        var vm = angular.extend(this, {
            jobName: '',
            jobId: NaN
        });


        vm.jobId = resolveJobId($stateParams, $q);
        jobService.getById(vm.jobId)
        .then(function (job) {
            vm.jobName = job.name;
        }, function (e) {
            $log.warning('Error while getting job for asset sidebar nav', e);
        });
    }


    // resolves a job id from route state parameters
    function resolveJobId($stateParams, $q) {
        var jobId = $stateParams.jobId;
        if (angular.isNumber(jobId)) {
            return jobId;
        } else if (!jobId) {
            $q.reject('jobId is required');
        } else if (angular.isString(jobId)) {
            return parseInt(jobId);
        }
        return $q.reject('Invalid job id.' + jobId);
    }
})();