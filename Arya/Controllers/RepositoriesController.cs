using System;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.OData;
using System.Web.OData.Routing;
using Eddard;

namespace Arya.Controllers
{
    /// <summary>
    /// This controller implements everything the OData Web API integration enables by hand.
    /// </summary>
    [ODataRoutePrefix("Repositories")]
    public class RepositoriesController : ODataController
    {
        // this example uses EntityFramework CodeFirst
        private DocumentsContext _ctx = new DocumentsContext();

        [EnableQuery]
        [ODataRoute]
        public IHttpActionResult Get()
        {
            return Ok(_ctx.Repositories.OrderBy(p => p.Name));
        }

        [EnableQuery]
        [ODataRoute("({id})")]
        public IHttpActionResult GetEntity(Guid id)
        {
            return Ok(SingleResult.Create(_ctx.Repositories.Where(p => p.Guid == id)));
        }

        protected override void Dispose(bool disposing)
        {
            _ctx.Dispose();
            base.Dispose(disposing);
        }
    }
}
