[![Build status](https://ci.appveyor.com/api/projects/status/c6025rmq05xbegvp/branch/master?svg=true)](https://ci.appveyor.com/project/OrleansContrib/orleans-storageprovider-arango/branch/master)

# Upgrade to support .NET Core
## Current status
I'm able to boot Orleans .NET Core with this as a provider, but haven't tested beyond that, yet.

## My task list
- [ ] Verify persistence works for one node
- [ ] Verify persistence works for many nodes
- [ ] Get the tests working
- [ ] Submit a PR to reintegrate

## Considerations
This is using a preview of Orleans, 2.0.0-preview4-20171025, [coming from here](https://dotnet.myget.org/F/orleans-prerelease).

# ArangoDB Storage Provider for Microsoft Orleans

Can be used to store grain state in an [ArangoDB](https://www.arangodb.com/) database.

## Installation

Install the [nuget package](https://www.nuget.org/packages/Orleans.StorageProvider.Arango):

```
PM> Install-Package Orleans.StorageProvider.Arango
```

## Usage

Register the provider like this:

```c#
config.Globals.RegisterArangoStorageProvider("ARANGO",
    url: "http://localhost:8529",
    username: "root",
    password: "password");
```

Then from your grain code configure grain storage in the normal way:

```c#
// define a state interface
public class MyGrainState
{
        string Value { get; set; }
}

// Select ARANGO as the storage provider for the grain
[StorageProvider(ProviderName="ARANGO")]
public class Grain1 : Orleans.Grain<MyGrainState>, IGrain1
{
        public Task Test(string value)
        {
                // set the state and save it
                this.State.Value = value;
                return this.WriteStateAsync();
        }

}
```

Note:

* Grain state can be stored using a database name of your choice. The default is 'Orleans'.
* The state is stored in a collection named after your grain. Alternatively, you can supply a table name as an argument.

## License

MIT
