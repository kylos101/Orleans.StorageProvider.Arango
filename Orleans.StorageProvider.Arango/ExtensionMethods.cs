using System.Collections.Generic;
using System.Text.RegularExpressions;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;

namespace Orleans.StorageProvider.Arango
{
    public static class ExtensionMethods
    {
        public static void RegisterArangoStorageProvider(
           this GlobalConfiguration globalConfig,
           string name,
           string databaseName = "Orleans",
           string url = "http://localhost:8529",
           string username = "root",
           string password = "",
           bool waitForSync = true,
           string collectionName = null)

        {
            var properties = new Dictionary<string, string>();

            properties.Add("DatabaseName", databaseName);
            properties.Add("Url", url);
            properties.Add("Username", username);
            properties.Add("Password", password);
            properties.Add("WaitForSync", waitForSync.ToString());
            properties.Add("CollectionName", collectionName);

            globalConfig.RegisterStorageProvider<ArangoStorageProvider>(name, properties);

        }
    }

    internal static class PrivateExtensions
    {
        static Regex documentKeyRegex = new Regex(@"[^a-zA-Z0-9_/-:.@(),=;$!*'%]");

        public static string ToArangoKeyString(this GrainReference grainRef)
        {
            return documentKeyRegex.Replace(grainRef.ToKeyString(), "_");
        }

        static Regex collectionRegex = new Regex(@"[^a-zA-Z0-9_-]");

        public static string ToArangoCollectionName(this string collectionName)
        {
            return documentKeyRegex.Replace(collectionName, "_");
        }
    }


}
