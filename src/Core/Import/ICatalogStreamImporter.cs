using Core.Domain;

namespace Core.Import;

public interface ICatalogStreamImporter
{
    IAsyncEnumerable<BookCopy> ReadAsync(
        string path,
        CancellationToken ct = default);
}
