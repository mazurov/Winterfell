using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.OData.Extensions;
using System.Web.OData.Formatter;
using Microsoft.OData.Core;
using WebApiContrib.Formatting;
using WebApiContrib.Formatting.Jsonp;

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
                routeName: "odata",
                routePrefix: "odata",
                model: ModelBuilder.GetEdmModel()
                );

            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "Eddard",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            //config.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            //config.Formatters.JsonFormatter.SerializerSettings.PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.None;
            config.AddJsonpFormatter(config.Formatters.JsonFormatter);

            config.Formatters.Remove(config.Formatters.XmlFormatter);
            //config.Formatters.Add(new ODataMediaTypeFormatter());

            //var cors = new EnableCorsAttribute("*", "*", "*");
            //config.EnableCors(cors);
        }
    }
}
