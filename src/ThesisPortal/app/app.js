(function () {
    'use strict';

    
    /**
     * @ngdoc overview
     * @name app
     * @description
     *
     * Proof of Concept
     */
    angular.module('app', [
        'ngAnimate',
        'ngSanitize',

        'ui.router',
        'ui.bootstrap',

        'toastr',
        'angularMoment',
        'indexedDB'
    ]);
    angular.module('app').run(function ($rootScope) {
        $rootScope.$on("$stateChangeError", console.log.bind(console));
    });



})();