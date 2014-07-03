using System;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.OData;
using System.Web.OData.Routing;
using Eddard;

namespace Jon.Controllers
{
    /// <summary>
    /// This controller implements everything the OData Web API integration enables by hand.
    /// </summary>
    [ODataRoutePrefix("Files")]
    public class FilesController : ODataController
    {
        // this example uses EntityFramework CodeFirst
        private DocumentsContext _ctx = new DocumentsContext();

        [EnableQuery]
        [ODataRoute]
        public IHttpActionResult Get()
        {
            return Ok(_ctx.Files);
        }

        [EnableQuery]
        [ODataRoute("({id})")]
        public IHttpActionResult GetEntity(Guid id)
        {
            return Ok(SingleResult.Create(_ctx.Files.Where(p => p.Guid == id)));
        }

     

        protected override void Dispose(bool disposing)
        {
            _ctx.Dispose();
            base.Dispose(disposing);
        }
    }
}
