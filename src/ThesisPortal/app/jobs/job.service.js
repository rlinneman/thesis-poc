(function () {
    'use strict';

    var componentId = 'jobService';

    angular.module('app').factory('jobService', jobService);

    function jobService($log, $q, $http, offlineStore, utils, toastr, principal) {
        var __reportedOffline = false,
            __reportedOnline = false,
            /**
             * @ngdoc object
             * @name app.jobService
             * @description 
             *
             * Exposes features oriented on Jobs.
             */
            service = {
                abandon: abandon,
                checkin: checkin,
                checkout: checkout,
                getAll: getAll,
                getById: getById,
                isLocked: isLocked,

                lockedByMe: lockedByMe,
                lockedByOther: lockedByOther,

                /*** Asset Utilities ***/
                getAssetAreas: getAssetAreas,
                getAssetByJobAndId: getAssetByJobAndId,
                getAssets: getAssets,
                save: save
            };

        return service;

        /**
         * @ngdoc function
         * @name app.jobService#abandon
         * @methodOf app.jobService
         * @description
         *
         * Drops all local changes and restores the job specified to an
         * online state.
         *
         * @param {Job} job The job to abandon offline changes from.
         * @returns {Q} A promise which, when resolved, signifies 
         * successful removal of all offline data for the job specified.
         */
        function abandon(job) {
            if (!job) { return $q.reject('Job is required'); }
            if (!job.offline) { return $q.reject('job is offline'); }

            // job was available locally so begin deleting all associated
            // data, working our way back up to delete the job last. This
            // way if there are any issues encountered, we don't end up with
            // a job partially on- and registered as fully off-line.
            return offlineStore.purge('asset', 'jobId', job.id, 'id')
                .then(function () { return offlineStore.purgeById('job', job.id); })
                .finally(clearOfflineStatus(job))
                .catch(handleOfflineError);
        }

        /**
         * @ngdoc function
         * @name app.jobService#checkin
         * @methodOf app.jobService
         * @description
         *
         * Assembles a change set of offline changes for a job and 
         * submits them for processing at the server.  If successful,
         * the job specified is returned to an online state.
         *
         * 
         *
         * @param {Job} job The job to check in offline changes from.
         * @returns {Q} A promise which, when resolved, signifies 
         * successful completion of a check in operation and subsequent
         * local cleanup of the offline store.
         */
        function checkin(job) {
            if (!job) { return $q.reject('Job is required'); }

            return compileChangeset(job)
                .then(function (changeSet) { return { partitionId: job.id, claimPartition: null, changeSet: changeSet }; })
                .then(sendChanges)
                .then(function (upd) {
                    // successful check in so abandon local changes as
                    // this job is now back online.
                    return abandon(job);
                }, function (j) {

                    job.reconcile = true;
                    offlineStore.put('job', job, true);
                    return $q.reject(job);
                });
        }

        /**
         * @ngdoc function
         * @name app.jobService#checkout
         * @methodOf app.jobService
         * @description
         *
         * Acquires a partition of data for offline processing.
         *
         * @param {Job} job The job to check out.
         * @returns {Q} A promise which, when resolved, signifies 
         * successful completion of a check out operation in whole.
         */
        function checkout(job) {
            if (!job) { return $q.reject('Job is required'); }

            var j = angular.copy(job);
            j.offline = true;
            j.offlineSince = new Date();

            return getAssets(job)
                    .then(function (r) {
                        return offlineStore.track('asset', r)
                            .then(function () {
                                offlineStore.track('job', j);
                            }, rethrowOfflineError)
                            .then(function () {
                                return angular.extend(job, j);
                            }, rethrowOfflineError);
                    }, rethrowOfflineError);
        }

        /**
         * @ngdoc function
         * @name app.jobService#getAll
         * @methodOf app.jobService
         * @description
         *
         * List all jobs available to the application.  
         *
         * @param {boolean=} [remoteTruth=undefined] A flag to indicate 
         * how offline and online changes should be treated. If `true` 
         * is specified, then online results will be yielded with only 
         * flags indicating the offline status of the job.  This is in 
         * contrast to returning offline results where the user may not
         * see any changes which have been made. The `true` behavior is
         * useful to render the project listing with information such
         * as pessimistic locks applied since checkout.
         *
         * @returns {Q} A promise which, when resolved, yields the list
         * of all jobs currently available to the application.
         */
        function getAll(remoteTruth) {
            var offlinePromise,
                onlinePromise;

            offlinePromise = offlineStore.getAll('job')
                .then(null, handleOfflineError);

            onlinePromise = $http.get('/api/jobs')
                .then(selectAndFixResponseData, handleOnlineError);

            return $q.all([offlinePromise, onlinePromise])
                .then(function (results) {
                    return mergeResults(results[0], results[1], remoteTruth === true);
                });
        }

        /**
         * @ngdoc function
         * @name app.jobService#getById
         * @methodOf app.jobService
         * @description
         *
         * Get a specific job (online or offline) by id.
         *
         * @param {number} id The identity of the job to acquire.
         * @returns {Q} A promise which, when resolved, yields the `Job`
         * requested. Or rejects with an error or not found.
         */
        function getById(id) {
            if (!id) { return $q.reject('Job ID is required'); }

            return offlineStore.get('job', id)
                .catch(function (e) { // cache miss
                    return $http.get('/api/jobs/' + id)
                        .then(selectAndFixResponseData);
                });
        }



        /**
         * @ngdoc function
         * @name app.jobService#isLocked
         * @methodOf app.jobService
         * @description
         *
         * Interprets the locked status of a job.
         *
         * @param {Job} job The job to inspect.
         * @returns {boolean} `true` if the `job` is locked; otherwise,
         * `false`.
         */
        function isLocked(job) {
            if (!job) { return $q.reject('Job is required'); }

            return !!job.lockedBy;
        }

        /**
         * @ngdoc function
         * @name app.jobService#lockedByMe
         * @methodOf app.jobService
         * @description
         *
         * Interprets the locked status of a job to determine if the 
         * identity specified holds a pessimistic lock on the given job.
         *
         * @param {Job} job The job to inspect.
         * @param {Identity} identity The identity of a user to test.
         * @returns {boolean} `true` if the `job` is locked by the
         * specified `identity`; otherwise, `false`.
         */
        function lockedByMe(job, identity) {
            return !!job.lockedBy && job.lockedBy === identity.username;
        }

        /**
         * @ngdoc function
         * @name app.jobService#lockedByOther
         * @methodOf app.jobService
         * @description
         *
         * Interprets the locked status of a job to determine if the 
         * identity specified does not hold the pessimistic lock on the 
         * given job.
         *
         * @param {Job} job The job to inspect.
         * @param {Identity} identity The identity of a user to test.
         * @returns {boolean} `true` if the `job` is locked and not by 
         * the specified `identity`; otherwise, `false`.
         */
        function lockedByOther(job, identity) {
            if (angular.isString(job)) {
                job = { lockedBy: job };
            }

            if (angular.isString(identity)) {
                identity = { username: identity };
            }
            return !!job.lockedBy && job.lockedBy !== identity.username;
        }

        /**
         * @ngdoc function
         * @name app.jobService#getAssetAreas
         * @methodOf app.jobService
         * @description
         *
         * Gets all known asset areas for the given job.
         *
         * @param {Job} job The job to find asset areas for.
         * @returns {Q} A promise which, when resolved, yields an Array
         * of distinct strings representing the known asset areas.
         */
        function getAssetAreas(job) {
            if (job.offline) {
                return offlineStore.query('asset', function (store) {
                    var query = store
                        .query()
                        .$index('jobId, serviceArea')
                        .$asc(true);
                    return store.eachWhere(query).then(function (a) {
                        return a.filter(function (i) { return i.jobId === job.id; });
                    });
                }).then(function (a) {
                    var r = [];
                    a.reduce(function (p, c, i) {
                        var v = c.serviceArea;
                        if (!p.hasOwnProperty(v)) {
                            r.push(c.serviceArea);
                            p[v] = c;
                        }
                        return p;
                    }, {});

                    return r;
                });
            }

            return $http({
                url: '/api/assets/areas',
                params: { jobId: job.id },
                paramSerializer: '$httpParamSerializerJQLike'
            })
            .then(selectAndFixResponseData);
        }



        /**
         * @ngdoc function
         * @name app.jobService#getAssetByJobAndId
         * @methodOf app.jobService
         * @description
         *
         * Gets an asset (online or offline) by the job status and 
         * assetId.
         *
         * @param {Job} job The job containing the asset specified.
         * @returns {Q} A promise which, when resolved, yields the 
         * asset requested, or rejects on not found or error.
         */
        function getAssetByJobAndId(job, assetId) {
            if (job.offline) {
                return offlineStore.get('asset', assetId);
            }

            return $http({
                url: '/api/asset',
                params: { jobId: job.id, assetId: assetId },
                paramSerializer: '$httpParamSerializerJQLike'
            }).then(selectAndFixResponseData, function (e) {
                $log.error("failed getting asset " + assetId + " in job " + job.id, e);
            });
        }

        /**
         * @ngdoc function
         * @name app.jobService#getAssetByJobAndId
         * @methodOf app.jobService
         * @description
         *
         * Gets all assets by the id and offline status of the given 
         * job.
         *
         * @param {Job} job The job containing the assets to process.
         * @returns {Q} A promise which, when resolved, yields an Array
         * of all assets currently available to the requested `Job`.
         */
        function getAssets(job, areaFilter, suppressShadow) {
            if (job.offline) {
                if (areaFilter) {
                    return offlineStore.getByIndex('asset', 'jobId, serviceArea', 'id', function (a) {
                        return job.id === a.jobId && a.serviceArea === areaFilter;
                    });
                } else {
                    return offlineStore.getAll('asset', suppressShadow);
                }
            }
            return $http({
                url: '/api/assets',
                params: { jobId: job.id, area: areaFilter },
                paramSerializer: '$httpParamSerializerJQLike'
            })
            .then(selectAndFixResponseData);
        }

        function save(job, asset) {
            if (job.offline) {
                return offlineStore.put('asset', asset)
                .then(function () { return asset; }, function (e) {
                    $log.error('error while saving asset.', e);
                    return $q.reject(e);
                });
            }

            return $http.put('/api/asset', asset)
                .then(selectAndFixResponseData, rethrowOnlineError);
        }

        function handleOfflineError(e) {
            if (!__reportedOffline && e && angular.isString(e) && /unavailable/.test(e)) {
                toastr.warning('Offline storage is unavailable.');
                __reportedOffline = true;
            }
            $log.error('jobService offline error:', e);
        }
        function rethrowOfflineError(e) {
            handleOfflineError(e);
            return $q.reject(e);
        }
        function rethrowOnlineError(e) {
            handleOnlineError(e);
            return $q.reject(e);
        }
        function handleOnlineError(e) {
            if (!__reportedOnline && e && e.status === -1) {
                toastr.warning('Online storage is unavailable.');
                __reportedOnline = true;
            }
            if (e.status === 400) {
                toastr.warning(e.data.message, e.statusText);
                return e.data;
            }
            $log.error('jobService online error:', e);
        }



        


        

        function reconcile(changeset) {
            return changeset;
        }

        function pickCurrentFromChangeSetItem(csi) {
            return csi.afim;
        }

        function handleChangeSetError(e) {
            if (e.status === 400) {
                $q.reject("Changeset is invalid. " + JSON.stringify(selectAndFixResponseData(e)));
            } else if (e.status === 409) {
                toastr.warning("Conflicts were detected.  Please review your changes and re-submit",'Check in');
                var changeset = selectAndFixResponseData(e);
                var assets = changeset.assets.map(pickCurrentFromChangeSetItem);
                return offlineStore
                    .put('asset', assets, true)
                    .then(function () {
                        changeset.job.reconcile = true;
                        return $q.reject(offlineStore.track(changeset.job));
                    });
            } else {
                return $q.reject(e);
            }
        }

        function createChangeItems(a) {
            return a[1].map(join(a[0]));
        }
        function join(inner) {
            return function (outer) {
                var bfim = utils.findById(inner, outer.id);
                var item = { bfim: bfim, afim: outer, action: 'update' };
                $log.debug('submitting change item ', item);
                return item;
            };
        }
        function filterToJob(job) {
            return function (a) {
                return a.filter(function (b) { return b.jobId === job.id; });
            };
        }
        function compileChangeset(job) {
            return $q.all([
                offlineStore.getAll('asset', 'id', true).then(filterToJob(job)),
                offlineStore.getAll('asset_shadow', 'id', true).then(filterToJob(job))
            ])

                //offlineStore.getByIndex('asset', 'jobId', function (a) { return a.jobId === job.id; }, true),
                //offlineStore.getByIndex('asset_shadow', 'jobId', function (a) { return a.jobId === job.id; })])
                .then(createChangeItems)
                .then(function (changes) {
                    changes.prototype = Array.prototype;
                    return { assets: changes };
                });
        }

        function sendChanges(changeset) {
            return $http.post('/api/offline/checkin', changeset)
                .then(selectAndFixResponseData, handleChangeSetError);
        }



        function clearOfflineStatus(job) {
            return function () {

                delete job.offline;
                delete job.offlineSince;
                delete job.reconcile;
                return job;
            };
        }

        // { offlineError: e, onlineError: e, offline: data, online: data }
        function mergeResults(offline, online, favorOnline) {
            if (!angular.isArray(offline) && !angular.isArray(online)) {
                $log.error('Unable to access either online or offline data');
                return $q.reject('No offline data available and unable to access online store');
            }

            offline = angular.isArray(offline) ? offline : [];
            online = angular.isArray(online) ? online : [];
            offline.unshift.apply(offline, online);// merge both arrays into one
            var keys = {};

            // build an index of keys
            for (var i = 0; i < offline.length; i += 1) {
                if (keys.hasOwnProperty(offline[i].id)) {
                    var orig = keys[offline[i].id];
                    // overwriting all properties or just pulling in offline status
                    if (favorOnline) {
                        orig.offline = offline[i].offline;
                        orig.offlineSince = offline[i].offlineSince;
                    } else {
                        angular.extend(orig, offline[i]);
                    }
                    offline.splice(i, 1);
                    i -= 1;
                } else {
                    keys[offline[i].id] = offline[i];
                }
            }

            return offline;
        }

        function selectAndFixResponseData(response) {
            return utils.fixObjGraph(response.data);
        }
    }
})();