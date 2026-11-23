using Core.Domain;

namespace Core.Import;

public interface ICatalogImporter
{
    Task<IReadOnlyList<BookCopy>> LoadAsync(
        string path,
        CancellationToken ct = default);
}
