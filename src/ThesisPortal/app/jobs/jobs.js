(function () {
    'use strict';


    angular.module('app').config(function ($stateProvider, $urlRouterProvider) {
        $urlRouterProvider.otherwise('/jobs');




        $stateProvider
            .state('jobs', {
                url: '/jobs',
                abstract: true,
                templateUrl: '/app/jobs/jobs.html',
                controllerAs: 'vm',
                controller: function ($state, $scope, toastr, jobService) {
                    jobService.getAll(true).then(function (jobs) {
                        $scope.jobs = jobs;
                        return jobs;
                    }, function (e) {
                        toastr.error('An error occured while getting jobs.', 'Error', e);
                    });
                },
                resolve: {
                    identity: function (principal) { return principal.identity(); }
                },
            })
            .state('jobs.list', {
                url: '',
                templateUrl: '/app/jobs/jobs.list.html'
            })

            .state('job', {
                url: '/jobs/{jobId:[0-9]+}',
                abstract: true,
                templateUrl: '/app/jobs/job.layout.html',
                controllerAs: 'vm',
                controller: function (job, identity, $scope, jobService) {
                    var vm = this;
                    $scope.job = job;

                    updateLockedBy(vm, job, jobService, identity);
                    $scope.$watch('job.lockedBy', function () {
                        updateLockedBy(vm, job, jobService, identity);
                    });

                },
                resolve: {
                    identity: function (principal) { return principal.identity(); },
                    job: function ($q, $stateParams, $state, jobService, utils) {
                        var jobId = utils.coerceToInt($stateParams.jobId);
                        if (!jobId) { return $q.reject('job id is required.'); }

                        if ($state.job && $state.job.id === jobId) { return $q.when($state.job); }

                        return jobService.getById(jobId).then(function (job) { return ($state.job = job); });
                    }
                },
            }).state('job.details', {
                url: '',
                templateUrl: '/app/jobs/job.details.html',
                controllerAs: 'vm',
                controller: function (job, identity, jobService) {
                    var vm = angular.extend(this, job, {
                        error: null
                    });
                    updateLockedBy(vm, job, jobService, identity);
                }
            }).state('job.checkout', {
                url: '/checkout',
                templateUrl: '/app/jobs/job.checkout.html',
                controllerAs: 'vm',
                controller: 'CheckoutController',
                resolve: {
                    action: function () { return 'checkout'; }
                }
            }).state('job.checkin', {
                url: '/checkin',
                templateUrl: '/app/jobs/job.checkin.html',
                controllerAs: 'vm',
                controller: 'CheckoutController',
                resolve: {
                    action: function () { return 'checkin'; }
                }
            }).state('job.abandon', {
                url: '/abandon',
                templateUrl: '/app/jobs/job.abandon.html',
                controllerAs: 'vm',
                controller: 'CheckoutController',
                resolve: {
                    action: function () { return 'abandon'; }
                }
            });

        function updateLockedBy(vm, job, jobService, identity) {
            vm.isLocked = jobService.isLocked(job);
            vm.lockedByOther = jobService.lockedByOther(job, identity);
            vm.lockedByMe = jobService.lockedByMe(job, identity);
        }

    });
})();