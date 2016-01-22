(function () {
    'use strict';

    var componentId = 'loginService';

    angular.module('app').factory(componentId, loginService);

    /**
     * @ngdoc object
     * @name app.loginService
     * @description 
     *
     * Acquires credentials to submit for authentication.
     */
    function loginService($q, $uibModal) {
        var service = {
            login: prompt
        };

        return service;

        /**
         * @ngdoc object
         * @name app.loginService#login
         * @methodOf app.loginService
         * @description 
         *
         * Acquires credentials to submit for authentication.
         *
         * This implementation uses Bootstrap to present a modal login
         * dialog prompt.
         *
         * @param {object} [seed] Provides seed values to the user 
         * credential acquisition process.
         *
         * @returns {Q} A promise which resolves with authenticated
         * user credentials.
         */
        function prompt(seed) {
            var inst = $uibModal.open({
                animation: true,
                templateUrl: '/app/auth/login.html',
                controller: 'LoginController as vm',
                size: 'sm',
                backdrop: 'static',
                resolve: {
                    username: function () {
                        return ((seed || {}).username || 'guest');
                    },
                    close: function () {
                        return function () {
                            return inst.close.apply(inst, arguments);
                        };
                    }
                }
            });



            return inst.result;
        }
    }
})();