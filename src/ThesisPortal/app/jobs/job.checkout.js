(function () {
    'use strict';

    var componentId = 'CheckoutController';
    angular.module('app').controller(componentId, CheckoutController);


    function CheckoutController(action, job, identity, $state, utils, jobService, toastr) {
        var vm = angular.extend(this, job, {
            error: null,

            locked: jobService.isLocked(job),
            lockedByOther: jobService.lockedByOther(job, identity),
            lockedByMe: jobService.lockedByMe(job, identity)
        });



        assertJobStatusAndAction(vm, action);


        function goHome() {
            $state.go('job.details');
        }

        function reconcile() {
            // a state yet to be defined
            goHome();
        }



        function abandon() {
            return utils.confirm('One last time... Are you sure you want to do this?').then(
                function () {
                    return jobService.abandon(job)
                            .then(function () {
                                toastr.info('Job wiped from offline storage.', job.name);
                                goHome();
                            });
                }, angular.noop);
        }



        function checkout() {
            if (vm.lockedByOther) {
                utils
                    .confirm('This job is currently locked by ' + job.lockedBy +
'. You have an increased chance of conflicts if you continue.  Click OK to accept the risk and checkout, or Cancel to cancel.')
                    .then(function () { proceedWithCheckout(job); }, angular.noop);
            } else {
                proceedWithCheckout();
            }
        }



        function proceedWithCheckout() {
            jobService
                .checkout(job)
                .then(function () {
                    toastr.info('Job checked out locally', job.name);
                    goHome();
                }, function (e) {
                    if (/locked by user/.test(e.exceptionMessage)) {
                        toastr.warning(e.exceptionMessage, job.name);
                    } else {
                        toastr.error('An error was encountered while attempting to check out this job.', job.name);
                    }
                });
        }


        function checkin() {
            jobService
                .checkin(job)
                .then(function (_) {
                    toastr.success("Your changes have been successfully checked in.");
                    goHome();
                }, function (e) {
                    if (e.staus === 400) {
                        if (/locked by user/.test(e.data.exceptionMessage)) {
                            toastr.error(e.data.exceptionMessage, job.name);
                        }
                        else if (e.message === 'conflict') {
                            return utils.confirm('One or more of your changes conflicts with changes already accepted at the server.  Please reconcile your changes and resubmit.')
                                .then(reconcile, goHome);
                        }
                    } else {
                        toastr.error('There was an error submitting your changes.', 'Unable to check in');
                    }
                });
        }


        function assertJobStatusAndAction(vm, action) {

            if (action === 'checkin') {
                vm.checkin = checkin;
                if (!job.offline) {
                    toastr.info("Job is not checked out.", job.name);
                    goHome();
                }
            } else if (action === 'checkout') {
                vm.checkout = checkout;
                if (job.offline) {
                    toastr.info("Job is already checked out.", job.name);
                    goHome();
                }
            } else if (action === 'abandon') {
                vm.abandon = abandon;
                if (!job.offline) {
                    toastr.info("Job is not checked out.", job.name);
                    goHome();
                }
            } else {
                throw 'invalid action';
            }
        }
    }
})();