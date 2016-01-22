(function () {
    'use strict';
    
    var componentId = 'LoginController';

    angular.module('app').controller(componentId, LoginController);


    /**
     * @ngdoc controller
     * @name app.LoginController
     * @description 
     *
     * Oversees the login process. 
     *
     * In this demonstration, the authentication is limited to only
     * notifying the server what the active username is.  We are not
     * interested in a full blown user account or profile and passwords.
     */
    function LoginController($q, authenticationService, toastr, username, close) {
        var vm = angular.extend(this, {
            username: username,
            login: login
        });

        authenticationService
            .identify()
            .then(sayHi)
            .then(close);
        /**
         * @ngdoc method
         * @name app.LoginController#login
         * @methodOf app.LoginController
         * @description 
         *
         * Attempts to login through the 
         * {@link app.authenticationService authenticationService} and
         *  close on success.
         *
         * @param {string} username The username to login under.

         */
        function login(username) {
            return authenticationService
                .login({ username: username })
                .then(sayHi)
                .then(close);
        }

        function sayHi(u) {
            toastr.success('Welcome ' + u.username);
            return u;
        }
    }
})();