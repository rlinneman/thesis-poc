(function () {
    'use strict';


    var componentId = 'AssetDetailsController';


    angular.module('app').controller(componentId, AssetDetailsController);


    function AssetDetailsController(job, asset, $log, $state, $scope, toastr, jobService, offlineStore, utils) {
        var vm = angular.extend(this, {
            submit: submit,
            reset: resetForm,
            reconcile: false
        });

        if (job.reconcile === true) {
            vm.reconcile = true;
            offlineStore
                .get('asset', asset.id, true)
                .then(function (_) { $scope.current = _; });
        }
        $scope.asset = angular.copy(asset);



        function goHome(msg, whereFrom) {
            toastr.error(msg, whereFrom || 'Asset Details');
            $state.go('job.assets');
        }

        function submit(userForm, editedAsset) {
            delete editedAsset.job;
            if (vm.reconcile) {
                if($scope.current){
                    editedAsset.rowVersion = $scope.current.rowVersion;
                }

            }
            jobService
                .save(job, editedAsset)
                .then(function (result) {
                    angular.extend(asset, result);
                    $scope.asset = angular.copy(asset);
                    userForm.$setPristine();
                    toastr.success('Changes saved.', 'Asset Details');
                }, function (e) {
                    //toastr.error('Error while saving', 'Asset Details');
                });
        }

        function resetForm(userForm, editedAsset) {
            angular.extend(editedAsset, asset);
            userForm.$rollbackViewValue();
            userForm.$setPristine();
        }
    }


})();