using Rel.Data;
using Rel.Data.Models;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Transactions;
using System.Web.Http;
using System.Web.Http.Description;

namespace ThesisPortal.Controllers
{
    [Authorize]
    public class AssetsController : ApiController
    {
        private readonly IDataContext _db;

        public AssetsController(IDataContext dataContext)
        {
            _db = dataContext;
        }

        [HttpGet]
        [Route("api/assets/areas")]
        public IEnumerable<string> GetAllAreas(int jobId)
        {
            return _db.Assets.GetAll()
                .Where(_ => _.JobId == jobId)
                .Select(_ => _.ServiceArea)
                .Distinct();
        }

        [HttpGet]
        [Route("api/assets")]
        public IQueryable<Asset> GetAssetsByJobAreaAndType(int jobId, string area = null)
        {
            if (area == null)
            {
                return _db.Assets.GetAll().Where(_ => _.JobId == jobId).AsQueryable();
            }
            else
            {
                return _db.Assets.GetAll().Where(_ => _.JobId == jobId && _.ServiceArea == area).AsQueryable();
            }
        }

        [HttpGet]
        [Route("api/asset")]
        [ResponseType(typeof(Asset))]
        public IHttpActionResult GetByJobAndId(int jobId, int assetId)
        {
            var asset = _db.Assets.GetAll().Where(_ => _.JobId == jobId && _.Id == assetId).SingleOrDefault();
            if (asset == null)
                return NotFound();

            return Ok(asset);
        }

        [HttpPut]
        [Route("api/asset")]
        [ResponseType(typeof(Asset))]
        public IHttpActionResult PutAsset(Asset asset)
        {
            using (var scope = new TransactionScope())
            {
                var job = _db.Jobs.GetById(asset.JobId);
                if (job == null)
                    return NotFound();

                if (job.LockedBy != null && !User.Identity.Name.Equals(job.LockedBy))
                    return BadRequest("Job locked");

                _db.Assets.Update(asset);

                try
                {
                    _db.AcceptChanges();
                    scope.Complete();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!Exists(asset))
                    {
                        return NotFound();
                    }
                    return BadRequest("Attempt to update with stale data.");
                }
            }
            return Ok(asset);
        }

        private bool Exists(Asset asset)
        {
            return _db
                .Assets
                .GetAll()
                .Where(_ => _.Id == asset.Id)
                .Any();
        }
    }
}