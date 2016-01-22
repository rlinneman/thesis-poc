using Rel.Data;
using Rel.Data.Bulk;
using System.Net;
using System.Net.Http;
using System.Transactions;
using System.Web.Http;
using System.Web.Http.Description;
using ThesisPortal.Models;

namespace ThesisPortal.Controllers
{
    [Authorize]
    public class OfflineController : ApiController
    {
        private ChangeSetProcessor _changeSetProcessor;

        public OfflineController(ChangeSetProcessor changeSetProcessor)
        {
            _changeSetProcessor = changeSetProcessor;
        }

        [HttpPost]
        [Authorize]
        [ResponseType(typeof(ChangeSet))]
        public IHttpActionResult CheckIn(CheckinRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ChangeSet result = null;
            try
            {
                result = _changeSetProcessor
                    .Process(request.PartitionId, request.ClaimPartition, request.ChangeSet);
            }
            catch (ConcurrencyException)
            {
                return Conflict();
            }

            if (result == null || result.IsEmpty)
                return NoContent();

            return Conflict(result);
        }

        [HttpPost]
        [Authorize]
        [ResponseType(typeof(ChangeSet))]
        public IHttpActionResult CheckOut(int partitionId)
        {
            var cs = _changeSetProcessor.BuildInitialChangeSet(partitionId);
            if (cs.IsEmpty)
                return NoContent();

            return Ok(cs);
        }

        private IHttpActionResult Conflict(ChangeSet changeSet)
        {
            if (changeSet == null || changeSet.IsEmpty)
                return Conflict();
            else
                return ResponseMessage(
                    Request.CreateResponse(
                    HttpStatusCode.Conflict, changeSet));
        }
        private IHttpActionResult NoContent()
        {
            return ResponseMessage(new HttpResponseMessage(HttpStatusCode.NoContent));
        }
    }
}