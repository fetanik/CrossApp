using Core;
using Core.Abstractions;
using Core.Storage;

public sealed record StoreConfiguration(
    ILibraryStore Store,
    string BaseStoreName,
    bool CacheEnabled,
    string? FilePath);

public static class StoreFactory
{
    public static StoreConfiguration Create(
        string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        bool useFile =
            args.Any(
                arg => arg.Equals(
                    "--file",
                    StringComparison.OrdinalIgnoreCase));

        bool useCache =
            args.Any(
                arg => arg.Equals(
                    "--cache",
                    StringComparison.OrdinalIgnoreCase));

        string? dataPath = null;

        ILibraryStore baseStore;

        if (useFile)
        {
            dataPath =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "data",
                    "library.json");

            baseStore =
                new FileLibraryStore(
                    dataPath);
        }
        else
        {
            baseStore =
                new InMemoryLibraryStore(
                    SampleData.Copies());
        }

        ILibraryStore store =
            useCache
                ? new CachingLibraryStore(
                    baseStore,
                    Console.WriteLine)
                : baseStore;

        return new StoreConfiguration(
            store,
            baseStore.GetType().Name,
            useCache,
            dataPath);
    }
}