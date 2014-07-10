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
    [ODataRoutePrefix("PropValues")]
    public class PropValuesController : ODataController
    {
        // this example uses EntityFramework CodeFirst
        private DocumentsContext _ctx = new DocumentsContext();

        [EnableQuery]
        [ODataRoute]
        public IHttpActionResult Get()
        {
            return Ok(_ctx.PropValues.OrderBy(p => p.Value));
        }

        [EnableQuery]
        [ODataRoute("({id})")]
        public IHttpActionResult GetEntity(int id)
        {
            return Ok(SingleResult.Create(_ctx.PropValues.Where(p => p.PropertyValueId == id)));
        }

        //[EnableQuery]
        //[ODataRoute("PublicationsModels.GetPropertyByName(name={name})")]
        //public IHttpActionResult GetByLanguageRelated(string name)
        //{
        //    return Ok(SingleResult.Create(_ctx.Properties.Where(p => p.Name == name)));
        //}  


        protected override void Dispose(bool disposing)
        {
            _ctx.Dispose();
            base.Dispose(disposing);
        }
    }
}
