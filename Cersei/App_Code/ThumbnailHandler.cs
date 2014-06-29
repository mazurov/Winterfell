using System;
using System.Configuration;
using System.IO;
using System.Web;
using Catelyn;


namespace Cersei
{
    public class ThumbnailHandler : IHttpHandler
    {
        /// <summary>
        /// You will need to configure this handler in the Web.config file of your 
        /// web and register it with IIS before being able to use it. For more information
        /// see the following link: http://go.microsoft.com/?linkid=8101007
        /// </summary>

        #region IHttpHandler Members
        private MFilesApp _app;
        public bool IsReusable
        {
            // Return false in case your Managed Handler cannot be reused for another request.
            // Usually this would be false in case you have some state information preserved per request.
            get { return true; }
        }



        private  bool ConnectToMFiles(string host, string user, string password)
        {
            try
            {
                _app = MFilesApp.Create(host: host, user: user, pass: password);
            }
            catch (Exception ex)
            {
                //_logger.Error(ex.Message);
                throw ex;

            }

            return true;
        }

        private bool Initialize()
        {
            // ========================================================================================================
            // Connect to M-Files
            // ========================================================================================================
            if (!ConnectToMFiles(
                user: ConfigurationManager.AppSettings["MFilesUser"],
                password: ConfigurationManager.AppSettings["MFilesPassword"],
                host: ConfigurationManager.AppSettings["MFilesHost"]
                )) return false;
            return true;
        }

        public void ProcessRequest(HttpContext context)
        {
            
            if (_app == null)
            {
                if (!Initialize()) return;
                if (_app == null) return;

            }

            //write your handler implementation here.
            var vaultName = context.Request.Params["vault"];
            var fileName = Path.GetFileName(context.Request.Params["file"]);
            

            var vault = _app.GetVault(vaultName);
            if (null == vault) {
                context.Response.StatusCode = 404;
                return;
            }

            string cacheDir = context.Server.MapPath("~/App_Data/" + vaultName);
            string cacheImage = cacheDir + "/" + fileName + ".png";
            DateTime cacheDate = DateTime.MinValue;
            bool cacheExists = File.Exists(cacheImage);
            if (cacheExists)
            {
                cacheDate = File.GetCreationTime(cacheImage);
            }


            var image = Utils.GetFileThumbnail(vault,
                fileName:fileName, 
                width:210,
                height:297,
                cacheDate: cacheDate
             );

            if (null == image)
            {
                if (!cacheExists)
                {
                    context.Response.StatusCode = 404;
                    return;
                }
                context.Response.ContentType = "image/png";
                context.Response.BinaryWrite(File.ReadAllBytes(cacheImage));
            }
            else
            {
                context.Response.ContentType = "image/png";
                context.Response.BinaryWrite(image);
                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                }
                
                File.WriteAllBytes(cacheImage, image);
            }
        }

        #endregion
    }
}
