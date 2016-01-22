(function () {
    angular.module('app').directive('ccSidebar', function () {
        // Opens and clsoes the sidebar menu.
        // Usage:
        //  <div data-cc-sidebar>
        // Creates:
        //  <div data-cc-sidebar class="sidebar">
        var directive = {
            link: link,
            restrict: 'A'
        };
        return directive;

        function link(scope, element, attrs) {
            var dropClass = 'dropy';
            var $sidebarInner =    angular.element(element[0].querySelector('.sidebar-inner'));
            var $dropdownElement = angular.element(element[0].querySelector('.sidebar-dropdown a'));
            var par = angular.element($sidebarInner.parentElement);
            element.addClass('sidebar');
            $dropdownElement.on('click', dropdown);
            $sidebarInner.on('click', function (e) {
                if ($dropdownElement.hasClass(dropClass)) {
                    hideAllSidebars();
                }
                    
            });

            function dropdown(e) {
                e.preventDefault();
                if (!$dropdownElement.hasClass(dropClass)) {
                    hideAllSidebars();
                    $sidebarInner.addClass('active');
                    $dropdownElement.addClass(dropClass);
                } else if ($dropdownElement.hasClass(dropClass)) {
                    $sidebarInner.removeClass('active');
                    $dropdownElement.removeClass(dropClass);
                }

            }
                function hideAllSidebars() {
                    $sidebarInner.removeClass('active');
                    $dropdownElement.removeClass(dropClass);
                }
        }
    });
})();