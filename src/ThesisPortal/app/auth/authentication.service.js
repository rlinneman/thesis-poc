(function () {
    'use strict';

    var componentId = 'authenticationService';

    angular.module('app').factory(componentId, authenticationService);


    /**
     * @ngdoc object
     * @name app.authenticationService
     * @description 
     *
     * Manages communication with the server regarding authentication
     * information. This service is also responsible to handling 
     * offline user authentication.
     */
    function authenticationService($q, $timeout, $http, $log) {
        var service = {
            identify: identify,
            login: login,
            logoff: logoff
        };

        return service;

        /**
         * @ngdoc method
         * @name app.authenticationService#identify
         * @methodOf app.authenticationService
         * @description 
         *
         * Pings the server with a request for credential information
         * as the server knows. The end point of this request is 
         * authenticated, so, if the browser submits an existing auth
         * cookie back with this request, the result will be the 
         * authenticated username for the auth cookie used.  This 
         * empowers session continuation between browser reloads.
         *
         * @param {string} username The username to login under.

         */
        function identify() {
            return $http
                .get('api/auth/identify')
                .then(function (response) {
                    $log.debug(response);
                    return { username: response.data, authenticated: true };
                }, function (response) {
                    $log.debug(response);
                    return $q.reject(response);
                });
        }

        function login(credentials) {
            return $http.post('api/auth/in', credentials)
            .then(function (r) {
                return (r.data === true) ?
                    { username: credentials.username, authenticated: true } :
                    $q.reject(credentials);
            }, function (e) {
                // operating offline??
                if (e.status === -1) {
                    return { username: credentials.username, authenticated: true };
                }
                return $q.reject(e);
            });

        }

        function logoff() {
            return $http.post('api/auth/out');
        }
    }
})();