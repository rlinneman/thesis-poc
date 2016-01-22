(function () {
    'use strict';

    angular.module('app').factory('utils', utils);

    
    /**
     * @ngdoc object
     * @name app.utils
     * @description 
     *
     * Provides common utility methods.
     */
    function utils($uibModal) { 
        var service = {
            findById: findById,
            fixObjGraph: fixObjGraph,
            confirm: confirm,
            concatDistinct: concatDistinct,
            concatResults: concatResults,
            coerceToInt: coerceToInt,
            indexArray: indexArray
        };
        return service;

        




        /**
         * @ngdoc function
         * @name app.utils#coerceToInt
         * @methodOf app.utils
         * @description
         *
         * Converts a value to an integer.
         *
         * @param {*} value The value to coerce to numeric.
         * @returns {number} The integer represented by value or 0.
         */
        function coerceToInt(value) {
            return ~~value;
        }




        /**
         * @ngdoc function
         * @name app.utils#findById
         * @methodOf app.utils
         * @description
         *
         * Finds an object in an array by an arbitrary index value.
         *
         * @param {array} array The Array object to search.
         * @param {string} keyValue The identity value sought.
         * @param {string} [keyPath='id'] The property containing the key value.
         * @returns {object} The object identified by the given id.
         */
        function findById(array, id, keyPath) {
            keyPath = keyPath || 'id';
            for (var i = array.length; i--;) {
                if (array[i][keyPath] === id) {
                    return array[i];
                }
            }
        }




        /**
         * @ngdoc function
         * @name app:utils#indexArray
         * @methodOf app.utils
         * @description
         *
         * Indexes an array on an arbitrary property.
         *
         * @param {array} array The array to index.
         * @param {string} [key='id'] The property containing the index value.
         * @returns {object} An index object of the array.
         */
        function indexArray(array, key) {
            return array.reduce(function (p, c, i) {
                p[c[key]] = c;
                return p;
            }, {});
        }



        /**
         * @ngdoc function
         * @name app.utils#concatDistinct
         * @methodOf app.utils
         * @description
         *
         * Concatenates file to rank, ensuring that only one item with
         * an arbitrary key value exists in the resulting array.  If 
         * items `A` and `B` have key value `x` the item elected into
         * the resultant array is computed as follows:
         * 
         * * If `A` and `B` are both in `rank`, which ever appears last
         * is taken.
         * * If `A` is in `rank` and `B` is in `file`, `A` is taken.
         * * If `A` and `B` are both in `file`, which ever appears 
         * first is taken.
         * 
         * @param {array} rank An array of values.
         * @param {array} file An array of values.
         * @param {string} [key='id'] An optional keyPath to use in 
         * indexing the arrays.
         * @returns {array} An array containing all elements resolved
         * as distinct on the specified key.
         */
        function concatDistinct(rank, file, key) {
            var index = indexArray(rank, key);

            return rank
                .concat(file.filter(function (e, i, a) {
                    return !index.hasOwnProperty(e[key]);
                }));
        }

        

        /**
         * @ngdoc function
         * @name app.utils#concatResults
         * @methodOf app.utils
         * @description
         *
         * Wraps {@link app.utils#methods_concatDistinct concatDistinct}
         * as a delegate for convenience in `Q` chaining.
         *
         * @example query.find().then(concatResults(previousQueryResults, 'id'));
         *
         * @param {array} pred Predecessor query results. Will be used
         * as the rank value in concatDistinct.
         * @param {string} [keyPath] Indexing property of both arrays.
         */
        function concatResults(pred, keyPath) {
            return function (r) {
                return concatDistinct(asArray(pred), asArray(r), keyPath);
            };
        }
        function asArray(e) { return angular.isArray(e) ? e : []; }




        /**
         * @ngdoc function
         * @name app.utils#confirm
         * @methodOf app.utils
         * @description
         *
         * Presents an OK/Cancel dialog prompt.
         *
         * @param {string} question The question to prompt with.
         * @returns {Q} A promise which resolves when OK is chosen, or
         * rejects if canceled.
         */
        function confirm(question) {
            var inst = $uibModal.open({
                animation: true,
                template: '<div class="modal-header">' +
                         '   <h2>Confirm</h2>' +
                         '</div>' +
                         '<div class="modal-body">' +
                         '    <p>' + question + '</p>' +
                         '</div>' +
                         '<div class="modal-footer">' +
                         '    <div>' +
                         '        <button data-ng-click="vm.close()" class="btn btn-default">OK</button>' +
                         '        <button data-ng-click="vm.dismiss()" class="btn btn-default">Cancel</button>' +
                         '    </div>' +
                         '</div>',
                controllerAs: 'vm',
                controller: function (close, dismiss) {
                    this.close = close;
                    this.dismiss = dismiss;
                },
                size: 'sm',
                backdrop: 'static',
                resolve: {
                    close: function () {
                        return function () {
                            return inst.close.apply(inst, arguments);
                        };
                    },
                    dismiss: function () {
                        return function () {
                            return inst.dismiss.apply(inst, arguments);
                        };
                    }
                }
            });

            return inst.result;
        }






        

        /**
         * @ngdoc function
         * @name app.utils#fixObjGraph
         * @methodOf app.utils
         * @description
         *
         * Restores a deserialized object graph which used the $id, 
         * $ref, and $values attribute convention to serialize cycles.
         *
         * @param {object} graph The root of the deserialized graph.
         * @param {object} [indices={}] A pre-seeded index of $refs 
         * located in graph
         * @returns {object} The corrected object graph.
         */
        function fixObjGraph(graph, indices) {
            indices = indices || {};

            if (!graph || typeof graph === 'string') { return graph; }

            if (graph.hasOwnProperty('$ref')) {
                return indices[graph.$ref];
            }

            if (graph.hasOwnProperty('$id')) {
                if (graph.hasOwnProperty('$values')) {
                    graph = indices[graph.$id] = graph.$values;
                } else {
                    indices[graph.$id] = graph;
                    delete graph.$id;
                }
            }

            for (var member in graph) {
                if (graph.hasOwnProperty(member)) {
                    graph[member] = fixObjGraph(graph[member], indices);
                }
            }

            return graph;
        }
    }
})();