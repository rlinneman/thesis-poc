(function () {
    'use strict';

    var componentId = 'principal',
        _identity = void 0;

    angular.module('app').factory(componentId, principal);

    /**
     * @ngdoc object
     * @name app.principal
     * @description 
     *
     * Manages the authenticated user credentials.
     */
    function principal($q, loginService) {
        var service = {
            identity: identity
        };

        return service;

        /**
         * @ngdoc function
         * @name app.principal#identity
         * @methodOf app.principal
         * @description 
         *
         * Get the authenticated user credentials.
         *
         * @param {boolean=} [flush=undefined] Flags principal to drop
         * cached credentials and acquire new credentials from the 
         * loginService.
         */
        function identity(flush) {
            if (flush === true) { _identity = void 0; }


            return _identity || (_identity = loginService
                    .login({ username: 'guest' })
                    .then(setIdentity));


        }

        function setIdentity(identity) {
            return (_identity = $q.when(identity));
        }
    }
})();