using Rel.Data;
using Rel.Data.Ef6;
using Rel.Data.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Web.Http;
using System.Web.Http.Description;
using System;

namespace ThesisPortal.Controllers
{
    [Authorize]
    public class JobsController : ApiController
    {
        private IDataContext _db;

        public JobsController(IDataContext dataContext)
        {
            _db = dataContext;
        }

        // GET: api/Jobs/5
        [ResponseType(typeof(Job))]
        public IHttpActionResult GetJob(int id)
        {
            try
            {
                var job = _db.Jobs.GetAll().SingleOrDefault(_ => _.Id == id);
                if (job == null)
                {
                    return NotFound();
                }

                return Ok(job);
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }

        // GET: api/Jobs
        [ResponseType(typeof(Job[]))]
        public IHttpActionResult GetJobs()
        {
            try { 
            return Ok(_db.Jobs.GetAll().ToArray());
            }
            catch (Exception ex)
            {
                return Ok(ex.GetType().FullName);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}