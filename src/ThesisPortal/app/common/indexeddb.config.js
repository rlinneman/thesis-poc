(function () {
    'use strict';

    var shadow = '_shadow',
        isShadow = new RegExp(shadow + '$');


    angular.module('app').config(function ($indexedDBProvider) {
        $indexedDBProvider
            .connection('thesis')
            .upgradeDatabase(1, function (event, db, tx) {
                createAndShadowCopy(db, 'job', { keyPath: 'id' }, [
                    ['name_idx', 'name', { unique: false }]
                ]);
                createAndShadowCopy(db, 'asset', { keyPath: 'id' }, [
                                    ['jobId, serviceArea', ['jobId', 'serviceArea'], { unique: false }],
                                    ['jobId', 'jobId', { unique: false }]
                ]);
            });


        function createAndShadowCopy(db, storeName, optionalParams, indices) {
            var objectStore = db.createObjectStore(storeName, optionalParams);
            for (var i = 0; i < indices.length; i += 1) {
                objectStore.createIndex.apply(objectStore, indices[i]);
            }

            if (!isShadow.test(storeName)) {
                createAndShadowCopy(db, storeName + shadow, optionalParams, indices);
            }
        }
    });


    /**
     * @ngdoc object
     * @name app.offlineStore
     * @description
     *
     * Encapsulates the offline store and shadow copy functionality.
     * When interpreting methods within the offline store:
     * * TOP refers to the original values placed in the store.
     * * BOTTOM refers to the modified values in the store.
     *
     * In the store, unless specified otherwise, BOTTOM always 
     * supersedes TOP in read operations.
     */
    angular.module('app').factory('offlineStore', offlineStore);

    function offlineStore($q, $log, $indexedDB, utils) {
        var enabled = checkEnabled(),
            disabled = $q.reject('offline storage is unavailable'),
            svc = {
                get: get,
                query: query,
                getByIndex: getByIndex,
                put: put,
                getAll: getAll,
                track: track,
                purge: purgeByIndex,
                purgeById: purgeById
            };

        return svc;


        /**
         * @ngdoc function
         * @name app.offlineStore#get
         * @methodOf app.offlineStore
         * @description
         *
         * Gets an item from the store. 
         *
         * @param {string} type The type of object/store to pull from.
         * @param {string} id The identity of the item.
         * @param {boolean=} suppressShadow `true` to suppress the 
         * shadow copy logic.
         * @returns {Q} A promise which, when resolved, yields an item 
         * from the store or rejects when not found.
         */
        function get(type, id, suppressShadow) {
            if (!enabled) { return disabled; }
            var promise =
                suppressShadow === true ?
                $q.reject() :
                get(type + shadow, id, true);

            return promise
                .then(null, function () {
                    return openStore(type, _get(id));
                });
        }


        /**
         * @ngdoc function
         * @name app.offlineStore#query
         * @methodOf app.offlineStore
         * @description
         *
         * Gets zero or more items from the store. 
         *
         * @param {string} type The type of object/store to pull from.
         * @param {function} callback A callback which receives a store
         * and returns a {Q} of items from the store.
         * @param {boolean=} suppressShadow `true` to suppress the 
         * shadow copy logic.
         * @returns {Q} A promise which, when resolved, yields all
         * items from the store which satisfy the query.
         */
        function query(type, callback, suppressShadow) {
            if (!enabled) { return disabled; }
            var promise =
                suppressShadow === true ?
                $q.when([]) :
                getAll(type + shadow, callback, true);

            return promise
                .then(function (pred) {
                    return openStore(type, callback)
                        .then(function (a) { return pred.concat(a); });
                });
        }

        /**
         * @ngdoc function
         * @name app.offlineStore#getAll
         * @methodOf app.offlineStore
         * @description
         *
         * Gets all items from the store. 
         *
         * @param {string} type The type of object/store to pull from.
         * @param {string=} keyPath The key property to resolve
         * identity of items from.
         * @param {boolean=} suppressShadow `true` to suppress the 
         * shadow copy logic.
         * @returns {Q} A promise which, when resolved, yields all
         * items from the store.
         */
        function getAll(type, keyPath, suppressShadow) {
            if (!enabled) { return disabled; }
            var promise =
                suppressShadow === true ?
                $q.when([]) :
                getAll(type + shadow, keyPath, true);

            return promise
                .then(function (pred) {
                    return openStore(type, function (store) {
                        return store.getAll()
                            .then(utils.concatResults(pred, keyPath));
                    });
                });
        }



        /**
         * @ngdoc function
         * @name app.offlineStore#getByIndex
         * @methodOf app.offlineStore
         * @description
         *
         * Gets all items from the `type` store using a the specified 
         * `index` for query and which satisfy the given `filter`.
         * Returns a promise wrapping the query of all items in the
         * specified type store with a matching index. Items will be 
         * returned from both shadow copy and original values regions.
         * Each item will exist only once, if it exists in the shadow
         * copy region, the shadow will be returned.  Otherwise, the
         * original values are yielded.
         *
         * @param {string} type The type of object/store to pull from.
         * @param {string} index The index name to crawl.
         * @param {string=} keyPath The key property to resolve
         * identity of items from.
         * @param {function=} filter A callback to filter the results.
         * @param {boolean=} suppressShadow `true` to suppress the 
         * shadow copy logic.
         * @returns {Q} A promise which, when resolved, yields all
         * items from the store discovered by `index` passing the 
         * specified `filter` and found distinct by `keyPath`.
         */
        function getByIndex(type, index, keyPath, filter, suppressShadow) {
            if (!enabled) { return disabled; }
            var promise =
                suppressShadow === true ?
                $q.when([]) :
                getByIndex(type + shadow, index, keyPath, filter, true);
            $log.debug('getByIndex', arguments);
            return promise
                .then(function (pred) {
                    return openStore(type, _getByIndex(index, _filter(filter)))
                        .then(function (p) {
                            return utils.concatResults(pred, keyPath)(p);
                        });
                });
        }



        /**
         * @ngdoc function
         * @name app.offlineStore#purgeById
         * @methodOf app.offlineStore
         * @description
         *
         * Removes a specific item from the store. 
         *
         * @param {string} type The type of object/store to purge from.
         * @param {object} id The identity of the object.
         * @param {boolean=} suppressShadow `true` to suppress the 
         * shadow copy logic.
         * @returns {Q} A promise which, when resolved, signifies 
         * completion of the purge.
         */
        function purgeById(type, id, suppressShadow) {
            if (!enabled) { return disabled; }
            var promise =
                suppressShadow === true ?
                $q.when() :
                purgeById(type + shadow, id, true);

            return promise
                .finally(openAndDeleteStoreById(type, id));
            //.catch(angular.noop);
        }

        /**
         * @ngdoc function
         * @name app.offlineStore#purgeByIndex
         * @methodOf app.offlineStore
         * @description
         *
         * Removes all items from the store where the given `dbIndex`
         * equals the specified `dbIndexValue`.
         *
         * @param {string} type The type of object/store to purge from.
         * @param {string} dbIndex The index to crawl.
         * @param {*} dbIndexValue The value to find.
         * @param {string} [keyPath='id'] The identity property of the 
         * items to be deleted.
         * @param {boolean=} suppressShadow `true` to suppress the 
         * shadow copy logic.
         * @returns {Q} A promise which, when resolved, signifies 
         * completion of the purge.
         */
        function purgeByIndex(type, dbIndex, dbIndexValue, keyPath, suppressShadow) {
            if (!enabled) { return disabled; }
            var promise =
                suppressShadow === true ?
                $q.when() :
                purgeByIndex(type + shadow, dbIndex, dbIndexValue, keyPath, true);

            return promise.finally(openAndDeleteStore(type, dbIndex, dbIndexValue, keyPath));
        }


        /**
         * @ngdoc function
         * @name app.offlineStore#put
         * @methodOf app.offlineStore
         * @description
         *
         * Insert or update one or more items in the store.
         *
         * @param {string} type The type of object/store of the items.
         * @param {object|array} items The items to be placed in the store.
         * @param {boolean=} suppressShadow `true` to suppress the 
         * shadow copy logic.
         * @returns {Q} A promise which, when resolved, signifies the
         * successful completion of the insert or update operation.
         */
        function put(type, items, suppressShadow) {
            if (!enabled) { return disabled; }
            if (suppressShadow !== true) {
                type = type + shadow;
            }

            return openStore(type, function (store) {
                return store.upsert(items);
            });
        }



        function openAndDeleteStore(storeName, dbIndex, dbIndexValue, keyPath) {
            return function () {
                return openStore(storeName, function (store) {
                    var find = store.query().$eq(dbIndexValue).$index(dbIndex);
                    return store.eachWhere(find).then(function (a) { return deleteThese(a, keyPath, store); });
                });
            };
        }



        function openAndDeleteStoreById(storeName, itemKey) {
            return function () {
                return openStore(storeName, function (store) {
                    return store.delete(itemKey);
                });
            };
        }


        function deleteThese(a, keyPath, store) {
            var keys = a.map(function (e) { return e[keyPath]; });
            return $q.all(keys.map(store.delete, store));
        }


        function track(type, items) {
            return openStore(type, function (store) {
                return store.upsert(items);
            });
        }


        function _get(id) { return function (store) { return store.find(id); }; }


        function _select(a) { return a; }


        function _filter(filterFn) {
            return function (a) {
                var results = a.filter(filterFn);
                $log.debug('_filter %o -> %o', arguments, results);
                return results;
            };
        }


        function _getByIndex(index, filter) {
            return function (store) {
                return store.eachBy(index).then(filter);
            };
        }

        function logError(e) {
            $log.log(e, e);
            return $q.reject(e);
        }






        function openStore(name, callback) {
            var promise;
            if (angular.isFunction(name)) {
                callback = name;
                name = 'job';
            } else {
                name = name || 'job';
            }

            try {
                promise = $indexedDB.openStore(name, callback);
            } catch (e) {
                return $q.reject(e);
            }

            return promise;
        }

        function checkEnabled() {
            return true;//TODO
        }
    }
})();