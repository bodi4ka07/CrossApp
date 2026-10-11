using Core;
using Core.Abstractions;
using Core.Storage;

/// <summary>
/// Фабрика сховищ: вибір реалізації за аргументами командного рядка.
/// Лежить у Cli, а не в Core, бо знає конкретні класи (це частина composition root).
/// </summary>
internal static class StoreFactory
{
    public static string DataPath { get; } =
        Path.Combine(AppContext.BaseDirectory, "data", "library.json");

    public static ILibraryStore Create(string[] args)
    {
        ILibraryStore store = args.Contains("--file")
            ? new FileLibraryStore(DataPath)
            : new InMemoryLibraryStore(SampleData.Copies(), SampleData.Readers());

        return args.Contains("--cache") ? new CachingLibraryStore(store) : store;
    }
}
