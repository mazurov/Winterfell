using System.Web.Http;
using System.Web.Cors;
using System.Web.Http.Cors;
using System.Web.OData.Extensions;

namespace Jon
{

    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;



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

            var cors = new EnableCorsAttribute("*", "*", "*");
            config.EnableCors(cors);
        }
    }
}
