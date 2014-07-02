using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using MFilesAPI;

namespace Catelyn
{
    public static class Utils
    {

        public static Int64 GetUnixTimeStamp(DateTime date)
        {
            return (Int64)(date.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        public static IView GetView(IVault vault, string viewName)
        {
            return vault.ViewOperations.GetViews().Cast<IView>().FirstOrDefault(view => view.Name == viewName);
        }


        public static IEnumerable<ObjectVersion> GetObjectsByCondition(IVault vault, SearchConditions cond)
        {
            var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditionsEx(
                    SearchConditions: cond,
                    SearchFlags: MFSearchFlags.MFSearchFlagReturnLatestVisibleVersion,
                    SortResults: false,
                    MaxResultCount: 0
                    );
            return searchResults.Cast<ObjectVersion>().ToList();
        }

        public static TypedValue GetPropertyById(IVault vault, ObjVer obj, int id)
        {
            try
            {
                var res = vault.ObjectPropertyOperations.GetProperty(obj, id);
                return res.Value;
            }
            catch (COMException)
            {
                return null;
            }
        }

        public static byte[] GetFileThumbnail(IVault vault, string fileName, int width, int height, bool retDefault = true,
                    DateTime? cacheDate = null)
        {

            if (!cacheDate.HasValue)
            {
                cacheDate = DateTime.MinValue;
            }

            var objVer = GetObjectByFile(vault, fileName);
            if (objVer == null) return null;

            var objFile = GetFileByObject(vault, objVer);
            if (objFile == null) return null;

            if (objFile.ChangeTimeUtc <= cacheDate.Value)
            {
                return null;
            }


            return (byte[])vault.ObjectOperations.GetThumbnailAsBytes(
                objVer,
                objFile.FileVer,
                width,
                height,
                retDefault
            );
        }

        public static ObjectFile GetFileByObject(IVault vault, ObjVer objVer)
        {
            var files = vault.ObjectFileOperations.GetFiles(objVer);
            return files.Count < 1 ? null : files[1];
        }


        public static ObjVer GetObjectByFile(IVault vault, string fileName)
        {

            var cond = new SearchCondition();
            cond.ConditionType = MFConditionType.MFConditionTypeEqual;
            cond.Expression.SetFileValueExpression(MFFileValueType.MFFileValueTypeFileName);
            cond.TypedValue.SetValue(MFDataType.MFDatatypeText, fileName);

            var result = vault.ObjectSearchOperations.SearchForObjectsByCondition(cond, false);
            return result.Count < 1 ? null : result[1].ObjVer;
        }


        }

    public class MFilesApp
    {
        private MFilesServerApplication _server;

        private readonly string _user;
        private readonly string _pass;
        private readonly string _host;
        private readonly string _port;

        public static MFilesApp Create(string user, string pass, string host, string port = "2266")
        {
            var res = new MFilesApp(user, pass, host, port);
            res.Connect();
            return res;
        }

        private MFilesApp(string user, string pass, string host, string port = "2266")
        {
            _user = user;
            _pass = pass;
            _host = host;
            _port = port;
        }

        private void Connect()
        {
            _server = new MFilesServerApplication();
            _server.Connect(MFAuthType.MFAuthTypeSpecificMFilesUser,
                _user, _pass,
                null, "ncacn_ip_tcp",
                _host, _port,
                "", false);
        }

        public IVault GetVault(string name)
        {
            string[] svaults = { name };
            var vaults = GetVaults(svaults);
            return vaults.ContainsKey(name)? vaults[name]: null;
        }

        public IDictionary<string, Vault> GetVaults(string[] names)
        {
            var res = new Dictionary<string, Vault>();
            foreach (VaultOnServer serverVault in _server.GetVaults())
            {
                if (names.Contains(serverVault.Name))
                {
                    res.Add(serverVault.Name, serverVault.LogIn());
                }
            }
            return res;
       }

    }
}
