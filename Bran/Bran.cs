using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Interception;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using MFilesAPI;
using Catelyn;
using Eddard;
using Utility.Logging;
using Utility.Logging.NLog;
using JsonConfig;

namespace Bran
{

   


    class VaultPropertiesMapping : IEnumerable<KeyValuePair<string, int>>
    {
        private IVault _vault;
        private IDictionary<string, int> _dict;

        public VaultPropertiesMapping(string vaultName, IVault vault, dynamic properties)
        {
            _vault = vault;
            _dict = new Dictionary<string, int>();
            foreach (var prop in properties)
            {
                
                foreach (var v in prop.mapping.Keys)
                {
                    if (v == vaultName)
                    {
                        _dict[prop.name] = prop.mapping[v];
                    }
                }
             }
        }



        public int GetId(string name)
        {
            return _dict[name];
        }

        public TypedValue GetValue(string name, ObjVer obj)
        {
            var id = GetId(name);
            return Utils.GetPropertyById(_vault, obj, id);
        }

        public string GetDisplayValue(string name, ObjVer obj)
        {
            TypedValue value = GetValue(name, obj);
            return value != null? value.DisplayValue: "";
        }

        //public DateTime GetAsDateTime(string name, ObjVer obj)
        //{
        //    Timestamp time = GetValue(name, obj).GetValueAsTimestamp();
        //    return new DateTime(
        //        (int)time.Year, 
        //        (int)time.Month, 
        //        (int)time.Day, 
        //        (int)time.Hour, 
        //        (int)time.Minute, 
        //        (int)time.Second
        //     );
        //}
        public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    class Program
    {

        #region Nested classes to support running as service
        public const string ServiceName = "Bran";

        public class Service : ServiceBase
        {
            private Timer _timer;
            private int _intervalMs;

            public Service(int intervalMinutes)
            {
                ServiceName = ServiceName;
                _intervalMs = intervalMinutes * 60 * 1000;
            }

            protected override void OnStart(string[] args)
            {
                _logger.Info("Start service");
                _timer = new Timer { Interval = _intervalMs, AutoReset = true };
                _timer.Elapsed += timer_Elapsed;
                _timer.Enabled = true;

            }

            private void timer_Elapsed(object sender, ElapsedEventArgs e)
            {
                _timer.Enabled = false;
                _logger.Info("Start on timer");
                Program.Start(null);
                _timer.Enabled = true;
            }

            protected override void OnStop()
            {
                _timer.Stop();
                _timer = null;
                _logger.Info("Stop service");
            }
        }
        #endregion


        private static MFilesApp _mfiles;
        private static DocumentsContext _ctx;
        private static IDictionary<string, Vault> _vaults;
        private static ILogger _logger;

        private static ILogger GetLogger()
        {
            var loggerFactory = new NLogLoggerFactory();
            return loggerFactory.GetCurrentInstanceLogger();
        }



        private static bool ReadConfiguration(string configPath, string privateConfigPath)
        {
            _logger.Info("Read configuration: \n{0}\n{1}", configPath, privateConfigPath);

            try
            {
                var config = Config.ApplyJsonFromPath(configPath);
                Config.SetDefaultConfig(Config.ApplyJsonFromPath(privateConfigPath, config));
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
            }

            return true;
        }


        private static bool ConnectToMFiles(string host, string user, string password)
        {
            try
            {
                _logger.Info("Connect to M-Files ...");
                _mfiles = MFilesApp.Create(host: host, user: user, pass: password);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                return false;
            }

            return true;
        }

        private static bool ConnectToDatabase()
        {
            _logger.Info("Connect to database ...");
            //if (connectionString.Contains("LocalDB"))
            //{
            //    Database.SetInitializer(new DropCreateDatabaseIfModelChanges<DocumentsContext>());
            //}
            DbInterception.Add(new DbLogger());
            
            try
            {
                _ctx = new DocumentsContext();
                _ctx.Database.CreateIfNotExists();
                _ctx.Database.Connection.Open();
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                return false;
            }
            return true;
        }

        private static void ProcessVaults(string[] names, string viewName)
        {
            // _logger.Info("Process vaults ...");
            
            _vaults = _mfiles.GetVaults(names);


            foreach (var name in names)
            {
                if (!_vaults.ContainsKey(name))
                {
                    _logger.Warn("Could not find vault '{0}'", name);
                    return;
                }
                ProcessVault(name, _vaults[name], viewName);
            }

        }

        private static void ProcessVault(string name, Vault vault, string viewName)
        {
            _logger.Info("Process vault  '{0}'", name);
            var mapping = new VaultPropertiesMapping(name, vault, Config.Default.properties);


            var repository = _ctx.Repositories.FirstOrDefault(v => v.Name == name);
            if (repository == null)
            {
                _logger.Info("Create new vault '{0}'", name);
                repository = _ctx.Repositories.Create();
                repository.Guid = Guid.Parse(vault.GetGUID());
                repository.Name = name;

                _ctx.Repositories.Add(repository);
                SaveChanges();
            }
            ProcessObjects(repository, vault, mapping, viewName);
        }

        private static bool SaveChanges()
        {
            try
            {
                _ctx.SaveChanges();
                return true;
            }
            catch (DbEntityValidationException ex)
            {
                var sb = new StringBuilder();
                foreach (var failure in ex.EntityValidationErrors)
                {
                    sb.AppendFormat("{0} failed validation\n", failure.Entry.Entity.GetType());
                    foreach (var error in failure.ValidationErrors)
                    {
                        sb.AppendFormat("- {0} : {1}", error.PropertyName, error.ErrorMessage);
                        sb.AppendLine();
                    }
                }

                Exception exDb = new DbEntityValidationException(
                    "Entity Validation Failed - errors follow:\n" +
                    sb.ToString(), ex
                    ); // Add the original ex
                _logger.Error(exDb, sb.ToString());
                return false;
            }
        }

        private static void ProcessObjects(Repository repository, Vault vault, VaultPropertiesMapping map, string viewName)
        {
            int counterUpdated = 0;
            int counterProcessed = 0;
            IView view = Utils.GetView(vault, viewName);
            if (view == null)
            {
                _logger.Error("Could  not find view '{0}' for '{1}' vault", Config.Default.view, repository.Name);
                return;
            }

            foreach (var objVersion in Utils.GetObjectsByCondition(vault, view.SearchConditions))
            {
                counterProcessed++;

                ObjVer obj = objVersion.ObjVer;
                Guid guid = Guid.Parse(objVersion.ObjectGUID);
                Document ld = _ctx.Documents.Find(guid);

                if (ld != null && (ld.LastModified == objVersion.LastModifiedUtc))
                {
                    // Updating don't needed
                    continue;
                }

                if (ld != null && (ld.LastModified < objVersion.LastModifiedUtc))
                {
                    _logger.Info("Update object {0}", ld.NameOrTitle);
                    _ctx.Documents.Remove(ld);
                    SaveChanges();
                    ld = null;
                }

                if (ld == null)
                {
                    counterUpdated++;


                    ld = _ctx.Documents.Create();
                    ld.DocumentId = guid;
                    ld.Repository = repository;

                    // Mandatory fields
                    ld.NameOrTitle = map.GetDisplayValue("NameOrTitle", obj);
                    ld.Created = objVersion.CreatedUtc;
                    ld.LastModified = objVersion.LastModifiedUtc;
                    
                    _logger.Info("Create document '{0}'", ld.NameOrTitle);
                    _ctx.Documents.Add(ld);
                    
                    ProcessProperties(ld, obj, map);
                    ProcessPubDate(ld);
                    ProcessFile(ld, vault, obj);
                    SaveChanges();

                }
                
            }
            _logger.Info("{0} - updated {1} documents of {2}", repository.Name, counterUpdated, counterProcessed);
        }

        private static void ProcessPubDate(Document doc)
        {
            var dateProp = doc.Properties.FirstOrDefault(p => p.Property.Name == "PubDate");
            if (dateProp != null)
            {
                DateTime pubDate = DateTime.Parse(dateProp.Value);
                doc.PubDate = pubDate;
                return;
            }

            var yearProp = doc.Properties.FirstOrDefault(p => p.Property.Name == "PubYear");
            var monthProp = doc.Properties.FirstOrDefault(p => p.Property.Name == "PubMonth");

            if (yearProp != null && monthProp != null)
            {
                var pubDate = new DateTime(int.Parse(yearProp.Value), int.Parse(monthProp.Value), 1);
                doc.PubDate = pubDate;
                return;

            }
        }

        private static void ProcessFile(Document doc, Vault vault, ObjVer obj)
        {
            var files = vault.ObjectFileOperations.GetFiles(obj);
            foreach (ObjectFile f in files)
            {
                    var file = _ctx.Files.Create();
                    file.Guid = Guid.Parse(f.FileGUID);
                    file.Document = doc;
                    file.Extension = f.Extension;
                    file.Size = f.LogicalSize;
                    _ctx.Files.Add(file);
             
            }
        }

        private static void ProcessProperties(ObjectWithProperties ld, ObjVer obj, VaultPropertiesMapping map)
        {
            foreach (var keyvalue in map)
            {
                var dbProp = _ctx.Properties.FirstOrDefault(p => p.Name == keyvalue.Key);

                if (dbProp == null)
                {
                    dbProp = _ctx.Properties.Create();
                    dbProp.Name = keyvalue.Key;
                    _ctx.Properties.Add(dbProp);
                    SaveChanges();
                }

                TypedValue value = map.GetValue(dbProp.Name, obj);
                if ((value == null) || string.IsNullOrEmpty(value.DisplayValue))
                {
                    continue;
                }

                IList<PropValue> dbPropValues = new List<PropValue>();

                if ((value.DataType == MFDataType.MFDatatypeMultiSelectLookup) || (value.DataType == MFDataType.MFDatatypeLookup))
                {
                    IList<Lookup> lookups = new List<Lookup>();
                    if (value.DataType == MFDataType.MFDatatypeLookup)
                    {
                        lookups.Add(value.GetValueAsLookup());
                    }
                    else
                    {
                        foreach (Lookup ls in value.GetValueAsLookups())
                        {
                            lookups.Add(ls);
                        }
                    }
                    
                    foreach (Lookup l in lookups)
                    {
                        var dbPropValue =
                            _ctx.PropValues.FirstOrDefault(
                                pv => pv.Property.Name == dbProp.Name && pv.Value == l.DisplayValue);
                        if (dbPropValue == null)
                        {
                            dbPropValue = _ctx.PropValues.Create();
                            dbPropValue.Value = l.DisplayValue;
                            dbPropValues.Add(dbPropValue);
                        }

                        ld.Properties.Add(dbPropValue);
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(value.DisplayValue))
                    {
                        var dbPropValue = _ctx.PropValues.Create();
                        dbPropValue.Value = value.DisplayValue;
                        dbPropValues.Add(dbPropValue);
                        ld.Properties.Add(dbPropValue);
                    }

                }

                foreach (var pv in dbPropValues)
                {
                    pv.Property = dbProp;
                    _ctx.PropValues.Add(pv);
                }
                SaveChanges();
            }
        }


        static int Main(string[] args)
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            _logger = GetLogger();
            if (!Environment.UserInteractive)
            {
                // running as service
                _logger.Info("Service mode");
                using (var service = new Service(int.Parse(ConfigurationManager.AppSettings["timer"])))
                    ServiceBase.Run(service);
            }
            else
            {
                _logger.Info("Console mode");
                // running as console app
                Start(args);

                Console.WriteLine("Press any key to stop...");
                Console.ReadKey(true);

                Stop();
            }
            return 0;

        }

    private static void Start(string[] args)
    {

        _logger.Info("Start syncronization ...");
        // ========================================================================================================
        // Read configuration files
        // ========================================================================================================
        var configPath = Path.GetFullPath(ConfigurationManager.AppSettings["configPath"]);
        var privateConfigPath = Path.GetFullPath(ConfigurationManager.AppSettings["privateConfigPath"]);
        if (!ReadConfiguration(configPath, privateConfigPath)) return;

        // ========================================================================================================
        // Connect to M-Files
        // ========================================================================================================
        if (!ConnectToMFiles(
            user: Config.Default.mfiles.user,
            password: Config.Default.mfiles.password,
            host: Config.Default.mfiles.host
            )) return;

        // ========================================================================================================
        // Connect to datavase
        // ========================================================================================================
        if (!ConnectToDatabase()) return;
        // ========================================================================================================
        // Process Vaults
        // ========================================================================================================
        try
        {
            ProcessVaults(Config.Default.vaults, Config.Default.view);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, ex.Message);
        }
        }

    private static void Stop()
    {
        // onstop code here
    }
   }
}
