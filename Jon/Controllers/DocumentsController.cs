using System;
using System.Linq;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Routing;
using Eddard;

namespace Jon.Controllers
{
    /// <summary>
    /// This controller implements everything the OData Web API integration enables by hand.
    /// </summary>
    [ODataRoutePrefix("Documents")]
    public class DocumentsController : ODataController
    {
        // this example uses EntityFramework CodeFirst
        private DocumentsContext _ctx = new DocumentsContext();

        [EnableQuery]
        [ODataRoute]
        public IHttpActionResult Get()
        {
            return Ok(_ctx.Documents);
        }

        [EnableQuery]
        [ODataRoute("({id})")]
        public SingleResult<Document> GetEntity(Guid id)
        {
            return SingleResult.Create<Document>(_ctx.Documents.Where(d => d.DocumentId == id));
        }

        [EnableQuery]
        [ODataRoute("({id})/Eddard.GetRelated()")]
        public IHttpActionResult GetRelated([FromODataUri] Guid id)
        {
            Document item = _ctx.Documents.Find(id);
            if (item == null) return BadRequest("Document not found");


            PropValue val = item.Properties.FirstOrDefault(p => p.Property.Name == "UNNumber");
            if (val == null)
            {
                return BadRequest("UNNumber not founв in document");
            }

            return
                Ok(
                    _ctx.Documents.Where(
                        d => d.DocumentId != id && d.Properties.Any(p => p.Property.Name == "UnNumber" && p.Value == val.Value))
                   );

        }


        protected override void Dispose(bool disposing)
        {
            _ctx.Dispose();
            base.Dispose(disposing);
        }
    }
}
