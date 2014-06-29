using System.Web.OData.Builder;
using Microsoft.OData.Edm;
using Eddard;

namespace Arya
{
    /// <summary>
    /// Helper class to build the EdmModels by either explicit or implicit method.
    /// </summary>
    public static class ModelBuilder
    {
        /// <summary>
        /// Get the EdmModel
        /// </summary>
        public static IEdmModel GetEdmModel()
        {
            // build the model by convention
            return GetImplicitEdmModel();
            // or build the model by hand
            // return GetExplicitEdmModel();
        }

        /// <summary>
        /// Generates a model from a few seeds (i.e. the names and types of the entity sets)
        /// by applying conventions.
        /// </summary>
        /// <returns>An implicitly configured model</returns>        
        static IEdmModel GetImplicitEdmModel()
        {
            ODataModelBuilder modelBuilder = new ODataConventionModelBuilder();

            modelBuilder.EntitySet<Document>("Documents");
            modelBuilder.EntitySet<Property>("Properties");
            modelBuilder.EntitySet<PropValue>("PropValues");
            modelBuilder.EntitySet<Repository>("Repositories");

            var document =  modelBuilder.EntityType<Document>();

            //ActionConfiguration getRelated = modelBuilder.EntityType<Document>().Action("GetRelated");
            //getRelated.ReturnsCollectionFromEntitySet<Document>("Documents");


            FunctionConfiguration related = document.Function("GetRelated");
            related.ReturnsCollectionFromEntitySet<Document>("Documents");

            modelBuilder.Namespace = typeof(Document).Namespace;
            return modelBuilder.GetEdmModel();
        }

    }
}
