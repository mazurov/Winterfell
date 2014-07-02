using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Infrastructure.Interception;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Catelyn;
using Eddard;
using JsonConfig;
using MFilesAPI;
using Utility.Logging;
using Utility.Logging.NLog;

namespace Robb
{



    class Sync
    {
        private readonly Timer _timer;
        private int _interval;

        private static MFilesApp _mfiles;
        private static DocumentsContext _ctx;
        private static IDictionary<string, Vault> _vaults;
        private static ILogger _logger;


        private bool Configure()
        {
 
            // ========================================================================================================
            // Read configuration files
            // ========================================================================================================
            var configPath = Path.GetFullPath("settings.json");
            if (!ReadConfiguration(configPath)) return false;

            // ========================================================================================================
            // Connect to M-Files
            // ========================================================================================================
            if (!ConnectToMFiles(
                user: ConfigurationManager.AppSettings["MFilesUser"],
                password: ConfigurationManager.AppSettings["MFilesPassword"],
                host: ConfigurationManager.AppSettings["MFilesHost"]
                )) return false;

            // ========================================================================================================
            // Connect to database
            // ========================================================================================================
            if (!ConnectToDatabase()) return false;
         
            // ========================================================================================================
            // Set interval (minutes)
            // ========================================================================================================
            
            _interval = int.Parse(ConfigurationManager.AppSettings["interval"]) * 60 * 1000;
            _timer.Interval = _interval;
            
            return true;
        }

 
        public Sync()
        {

            _logger = GetLogger();
            _timer = new Timer(60 * 60 * 1000) { AutoReset = true};
            _timer.Elapsed += Process;
        }

        private void Process(object source, ElapsedEventArgs args)
        {
            _timer.Enabled = false;
            try
            {
                ProcessVaults(Config.Default.vaults, Config.Default.view);
                
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
            }
            _timer.Interval = _interval;
            _timer.Enabled = true;
        }


        public void Start()
        {
            if(Configure())
            {
                _timer.Interval = 5;
               _timer.Start();
            }
        }
        public void Stop() { _timer.Stop(); }


        private static ILogger GetLogger()
        {
            var loggerFactory = new NLogLoggerFactory();
            return loggerFactory.GetCurrentInstanceLogger();
        }



        private bool ReadConfiguration(string configPath)
        {
            _logger.Info("Read configuration: \n{0}", configPath);

            try
            {
                var config = Config.ApplyJsonFromPath(configPath);
                Config.SetDefaultConfig(config);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
            }

            return true;
        }


        private bool ConnectToMFiles(string host, string user, string password)
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

        private bool ConnectToDatabase()
        {
            _logger.Info("Connect to database ...");
            //if (connectionString.Contains("LocalDB"))
            //{
            //    Database.SetInitializer(new DropCreateDatabaseIfModelChanges<DocumentsContext>());
            //}
            // Database.SetInitializer(new DropCreateDatabaseIfModelChanges<DocumentsContext>());
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

                if (ld != null && (ld.LastModified == Utils.GetUnixTimeStamp(objVersion.LastModifiedUtc)))
                {
                    // Updating don't needed
                    continue;
                }

                if (ld != null && (ld.LastModified < Utils.GetUnixTimeStamp(objVersion.LastModifiedUtc)))
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
                    ld.Created = Utils.GetUnixTimeStamp(objVersion.CreatedUtc);
                    ld.LastModified = Utils.GetUnixTimeStamp(objVersion.LastModifiedUtc);

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
                doc.PubDate = Utils.GetUnixTimeStamp(pubDate);
                return;
            }

            var yearProp = doc.Properties.FirstOrDefault(p => p.Property.Name == "PubYear");
            var monthProp = doc.Properties.FirstOrDefault(p => p.Property.Name == "PubMonth");

            if (yearProp != null && monthProp != null)
            {
                var pubDate = new DateTime(int.Parse(yearProp.Value), int.Parse(monthProp.Value), 1);
                doc.PubDate = Utils.GetUnixTimeStamp(pubDate);
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

        private static void ProcessProperties(Document ld, ObjVer obj, VaultPropertiesMapping map)
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
    
    
    }

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
            return value != null ? value.DisplayValue : "";
        }


        public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }



}
