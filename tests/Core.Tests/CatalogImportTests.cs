using System.Text.Json;
using Core.Domain;
using Core.Exceptions;
using Core.Import;
using Core.Services;
using Core.Storage;
using Core.Tests.TestData;
using Core.Threading;

namespace Core.Tests;

public class CatalogImportTests
{
    [Fact]
    public async Task ImportAsync_PreCanceledToken_ThrowsAndDoesNotChangeStore()
    {
        var store = new InMemoryLibraryStore();
        var service = new LendingService(store);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.ImportAsync(
                new ListCatalogImporter(LibrarySamples.Copy("IMP-1")),
                "catalog.json",
                cts.Token));

        Assert.Empty(store.ListCopies());
    }

    [Fact]
    public async Task LoadAsync_PreCanceledToken_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var importer = new JsonCatalogImporter();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            importer.LoadAsync("missing.json", cts.Token));
    }

    [Fact]
    public async Task StrictImport_InvalidRecord_ThrowsDomainExceptionWithNumber()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            "crossapp-" + Guid.NewGuid().ToString("N") + ".json");

        try
        {
            await File.WriteAllTextAsync(
                path,
                """
                [
                  {"id":"OK-1","isbn":"9786171002001","title":"Добре","year":1999},
                  {"id":"BAD","isbn":"","title":"Порожній ISBN","year":2001}
                ]
                """);

            var importer = new JsonCatalogImporter();
            DomainException ex = await Assert.ThrowsAsync<DomainException>(
                () => importer.LoadAsync(path));

            Assert.Contains("Запис 2", ex.Message);
            Assert.IsAssignableFrom<ArgumentException>(ex.InnerException);

            var store = new InMemoryLibraryStore();
            var service = new LendingService(store);
            int events = 0;
            service.Changed += (_, _) => events++;

            await Assert.ThrowsAsync<DomainException>(() =>
                service.ImportAsync(importer, path));

            Assert.Empty(store.ListCopies());
            Assert.Equal(0, events);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task LoadAsync_MalformedJson_ThrowsJsonException()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            "crossapp-" + Guid.NewGuid().ToString("N") + ".json");

        try
        {
            await File.WriteAllTextAsync(path, "{ \"id\": ");
            var importer = new JsonCatalogImporter();

            await Assert.ThrowsAnyAsync<JsonException>(() => importer.LoadAsync(path));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task ImportAsync_SecondPass_AddsNothingAndRaisesNoExtraEvents()
    {
        var store = new InMemoryLibraryStore();
        var service = new LendingService(store);
        var importer = new ListCatalogImporter(
            LibrarySamples.Copy("IMP-1"),
            LibrarySamples.Copy("IMP-2"),
            LibrarySamples.Copy("IMP-1"));

        int events = 0;
        service.Changed += (_, _) => events++;

        int added = await service.ImportAsync(importer, "catalog.json");
        int addedAgain = await service.ImportAsync(importer, "catalog.json");

        Assert.Equal(2, added);
        Assert.Equal(0, addedAgain);
        Assert.Equal(2, events);
        Assert.Equal(2, store.ListCopies().Count);
        Assert.Equal(["IMP-1", "IMP-2"], store.ListCopies().Select(copy => copy.Id).OrderBy(id => id));
    }

    [Fact]
    public async Task ImportAsync_ReportsPercentFromZeroToHundred()
    {
        var service = new LendingService(new InMemoryLibraryStore());
        var importer = new ListCatalogImporter(
            LibrarySamples.Copy("P-1"),
            LibrarySamples.Copy("P-2"),
            LibrarySamples.Copy("P-3"));
        var progress = new ProgressList();

        int added = await service.ImportAsync(
            importer,
            "catalog.json",
            progress: progress);

        Assert.Equal(3, added);
        Assert.Equal([0, 33, 66, 100], progress.Values);
        Assert.All(progress.Values, value => Assert.InRange(value, 0, 100));
    }

    [Fact]
    public async Task ImportStreamAsync_ReadsFileAndSkipsDuplicates()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            "crossapp-" + Guid.NewGuid().ToString("N") + ".json");

        try
        {
            await File.WriteAllTextAsync(
                path,
                """
                [
                  {"id":"S-1","isbn":"9786171002201","title":"Перша","year":2001},
                  {"id":"S-2","isbn":"9786171002202","title":"Друга","year":2002}
                ]
                """);

            var store = new InMemoryLibraryStore();
            var service = new LendingService(store);
            var importer = new JsonCatalogImporter();
            var progress = new ProgressList();
            int events = 0;
            service.Changed += (_, _) => events++;

            int added = await service.ImportStreamAsync(importer, path, processed: progress);
            int addedAgain = await service.ImportStreamAsync(importer, path);

            Assert.Equal(2, added);
            Assert.Equal(0, addedAgain);
            Assert.Equal(2, events);
            Assert.Equal([1, 2], progress.Values);
            Assert.Equal(2, store.ListCopies().Count);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task ImportStreamAsync_InvalidLaterRecord_DoesNotAddThatRecord()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            "crossapp-" + Guid.NewGuid().ToString("N") + ".json");

        try
        {
            await File.WriteAllTextAsync(
                path,
                """
                [
                  {"id":"S-1","isbn":"9786171002301","title":"Перша","year":2001},
                  {"id":"S-2","isbn":"","title":"Друга","year":2002}
                ]
                """);

            var store = new InMemoryLibraryStore();
            var service = new LendingService(store);
            int events = 0;
            service.Changed += (_, _) => events++;

            DomainException ex = await Assert.ThrowsAsync<DomainException>(() =>
                service.ImportStreamAsync(new JsonCatalogImporter(), path));

            Assert.Contains("Запис 2", ex.Message);
            Assert.Equal(["S-1"], store.ListCopies().Select(copy => copy.Id));
            Assert.Equal(1, events);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task LinkedTimeout_CancelsOperationWhileUserTokenStaysActive()
    {
        using var userCts = new CancellationTokenSource();
        using var timeoutCts = new CancellationTokenSource();
        using CancellationTokenSource linked = LinkedCancellation.Link(
            userCts.Token,
            timeoutCts.Token);

        timeoutCts.Cancel();

        OperationCanceledException ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Task.Delay(Timeout.Infinite, linked.Token));

        Assert.False(userCts.IsCancellationRequested);
        Assert.True(timeoutCts.IsCancellationRequested);
        Assert.True(linked.IsCancellationRequested);
        Assert.Equal(linked.Token, ex.CancellationToken);
    }

    private sealed class ListCatalogImporter : ICatalogImporter
    {
        private readonly IReadOnlyList<BookCopy> _copies;

        public ListCatalogImporter(params BookCopy[] copies)
        {
            _copies = copies;
        }

        public Task<IReadOnlyList<BookCopy>> LoadAsync(
            string path,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(_copies);
        }
    }

    private sealed class ProgressList : IProgress<int>
    {
        public List<int> Values { get; } = [];

        public void Report(int value) => Values.Add(value);
    }
}
