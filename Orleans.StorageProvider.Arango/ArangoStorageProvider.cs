using System;
using System.Threading.Tasks;

using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Storage;

using ArangoDB.Client;
using Orleans.StorageProvider.Arango;

using System.Collections.Concurrent;

using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Orleans.StorageProvider.Arango
{
    public class ArangoStorageProvider : IStorageProvider
    {
        public ArangoDatabase Database { get; private set; }

        public Logger Log { get; private set; }

        public string Name { get; private set; }

        static Newtonsoft.Json.JsonSerializer jsonSerializerSettings;
        string collectionName;

        ConcurrentBag<string> initializedCollections = new ConcurrentBag<string>();

        public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            try
            {
                var primaryKey = grainReference.ToArangoKeyString();
                var collection = await GetCollection(grainType);

                await collection.RemoveByIdAsync(primaryKey).ConfigureAwait(false);

                grainState.ETag = null;
            }
            catch (Exception ex)
            {
                this.Log.Error(190002, "ArangoStorageProvider.ClearStateAsync()", ex);
                throw new ArangoStorageException(ex.ToString());
            }
        }

        public Task Close()
        {
            this.Database.Dispose();
            return Task.CompletedTask;
        }

        public Task Init(string name, IProviderRuntime providerRuntime, IProviderConfiguration config)
        {
            this.Log = providerRuntime.GetLogger(nameof(ArangoStorageProvider));
            this.Name = name;

            var databaseName = config.GetProperty("DatabaseName", "Orleans");
            var url = config.GetProperty("Url", "http://localhost:8529");
            var username = config.GetProperty("Username", "root");
            var password = config.GetProperty("Password", "");
            var waitForSync = config.GetBoolProperty("WaitForSync", true);
            collectionName = config.GetProperty("CollectionName", null);

            var serializationManager = providerRuntime.ServiceProvider.GetRequiredService<SerializationManager>();
            var grainRefConverter = new GrainReferenceConverter(serializationManager, providerRuntime.GrainFactory);

            ArangoDatabase.ChangeSetting(s =>
            {
                s.Database = databaseName;
                s.Url = url;
                s.Credential = new NetworkCredential(username, password);
                s.DisableChangeTracking = true;
                s.WaitForSync = waitForSync;
                s.Serialization.Converters.Add(grainRefConverter);
            });

            jsonSerializerSettings = new JsonSerializer();
            jsonSerializerSettings.Converters.Add(grainRefConverter);

            this.Database = new ArangoDatabase();

            return Task.CompletedTask;
        }

        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            try
            {
                var primaryKey = grainReference.ToArangoKeyString();
                var collection = await GetCollection(grainType);

                var result = await collection.DocumentAsync<IGrainState>(primaryKey).ConfigureAwait(false);
                if (null == result) return;

                if (result.State != null)
                {
                    grainState.State = (result.State as JObject).ToObject(grainState.State.GetType(), jsonSerializerSettings);
                }
                else
                {
                    grainState.State = null;
                }                
            }
            catch (Exception ex)
            {
                this.Log.Error(190000, "ArangoStorageProvider.ReadStateAsync()", ex);
                throw new ArangoStorageException(ex.ToString());
            }
        }

        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
             try
            {
                var primaryKey = grainReference.ToArangoKeyString();
                var collection = await GetCollection(grainType);                

                if (string.IsNullOrWhiteSpace(grainState.ETag))
                {
                    var result = await collection.InsertAsync(grainState.State).ConfigureAwait(false);                    
                }
                else
                {
                    var result = await collection.UpdateByIdAsync(primaryKey, grainState.State).ConfigureAwait(false);                    
                }
            }
            catch (Exception ex)
            {
                this.Log.Error(190001, "ArangoStorageProvider.WriteStateAsync()", ex);
                throw new ArangoStorageException(ex.ToString());
            }
        }

        private Task<IDocumentCollection> GetCollection(string grainType)
        {
            if (!string.IsNullOrWhiteSpace(this.collectionName))
            {
                return InitializeCollection(this.collectionName);
            }

            return InitializeCollection(grainType.Split('.').Last().ToArangoCollectionName());
        }

        private async Task<IDocumentCollection> InitializeCollection(string name)
        {
            if (!this.initializedCollections.Contains(name))
            {
                try
                {
                    await this.Database.CreateCollectionAsync(name);
                }
                catch (Exception)
                {
                    this.Log.Info($"Arango Storage Provider: Error creating {name} collection, it may already exist");
                }

                this.initializedCollections.Add(name);
            }

            return this.Database.Collection(name);
        }
    }
}
