(function () {
    'use strict';


    angular.module('app').config(function (toastrConfig) {
        angular.extend(toastrConfig, {
            positionClass: 'toast-bottom-center'
        });
    });

    

    angular.module('app').config(function ($logProvider) {
        $logProvider.debugEnabled(true);
    });


    angular.module('app').filter('numeric', function() {
        return function(input) {
            return parseInt(input, 10);
        };
    });
})();