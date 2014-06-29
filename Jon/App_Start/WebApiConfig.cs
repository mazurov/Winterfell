using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Cors;
using System.Web.OData.Extensions;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;
using Arya;
using Microsoft.OData.Edm;

namespace Jon
{

    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            // Enables OData support by adding an OData route and enabling querying support for OData.
            // Action selector and odata media type formatters will be registered in per-controller configuration only

            // var conventions = ODataRoutingConventions.CreateDefault();
            // conventions.Insert(0, new NavigationIndexRoutingConvention());

            config.MapODataServiceRoute(
                routeName: "OData",
                routePrefix: "odata",
                model: ModelBuilder.GetEdmModel()
                );

            config.MapHttpAttributeRoutes();

            config.Routes. MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "odata/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            var cors = new EnableCorsAttribute("*", "*", "*");
            config.EnableCors(cors);
        }
    }
}
