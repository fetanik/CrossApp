using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class FileLibraryStore : ILibraryStore
{
    private static readonly JsonSerializerOptions Options =
        new()
        {
            WriteIndented = true
        };

    private readonly Dictionary<string, BookCopy> _copies =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, Loan> _loans =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly string _path;

    private bool _loaded;

    public FileLibraryStore(string path)
    {
        _path = Path.GetFullPath(path);
    }

    private void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        if (File.Exists(_path))
        {
            string json =
                File.ReadAllText(_path);

            LibraryStateDto? state =
                JsonSerializer.Deserialize<LibraryStateDto>(
                    json,
                    Options);

            foreach (BookCopyStateDto dto
                     in state?.Copies ?? [])
            {
                BookCopy copy =
                    BookCopy.Create(
                        dto.Id,
                        dto.Isbn,
                        dto.Title,
                        dto.Year,
                        dto.Author);

                if (dto.IsIssued)
                {
                    copy.Issue();
                }

                _copies[copy.Id] = copy;
            }

            foreach (LoanStateDto dto
                     in state?.Loans ?? [])
            {
                Loan loan =
                    Loan.Open(
                        dto.Id,
                        dto.BookCopyId,
                        dto.ReaderId,
                        dto.IssuedOn);

                if (dto.Status == nameof(LoanStatus.Closed))
                {
                    if (dto.ReturnedOn is null)
                    {
                        throw new InvalidOperationException(
                            $"Видача {dto.Id} має статус Closed, " +
                            "але не має дати повернення.");
                    }

                    loan.Close(
                        dto.ReturnedOn.Value);
                }
                else if (
                    dto.Status ==
                    nameof(LoanStatus.Cancelled))
                {
                    loan.Cancel();
                }

                _loans[loan.Id] = loan;
            }
        }

        _loaded = true;
    }

    private void Flush()
    {
        Directory.CreateDirectory(
            Path.GetDirectoryName(_path)!);

        var state =
            new LibraryStateDto(
                _copies.Values
                    .Select(copy =>
                        new BookCopyStateDto(
                            copy.Id,
                            copy.Isbn,
                            copy.Title,
                            copy.Year,
                            copy.Author,
                            copy.IsIssued))
                    .ToList(),

                _loans.Values
                    .Select(loan =>
                        new LoanStateDto(
                            loan.Id,
                            loan.BookCopyId,
                            loan.ReaderId,
                            loan.IssuedOn,
                            loan.ReturnedOn,
                            loan.Status.ToString()))
                    .ToList());

        string json =
            JsonSerializer.Serialize(
                state,
                Options);

        File.WriteAllText(
            _path,
            json);
    }


    public IReadOnlyList<BookCopy> ListCopies()
    {
        EnsureLoaded();

        return _copies.Values.ToList();
    }

    public BookCopy? GetCopyById(string id)
    {
        EnsureLoaded();

        return _copies.GetValueOrDefault(id);
    }

    public void AddCopy(BookCopy copy)
    {
        ArgumentNullException.ThrowIfNull(copy);

        EnsureLoaded();

        if (_copies.ContainsKey(copy.Id))
        {
            throw new InvalidOperationException(
                $"Примірник з id={copy.Id} уже існує.");
        }

        _copies.Add(
            copy.Id,
            copy);

        Flush();
    }

    public void UpdateCopy(BookCopy copy)
    {
        ArgumentNullException.ThrowIfNull(copy);

        EnsureLoaded();

        _copies[copy.Id] = copy;

        Flush();
    }


    public IReadOnlyList<Loan> ListLoans()
    {
        EnsureLoaded();

        return _loans.Values.ToList();
    }

    public Loan? GetLoanById(string id)
    {
        EnsureLoaded();

        return _loans.GetValueOrDefault(id);
    }

    public void AddLoan(Loan loan)
    {
        ArgumentNullException.ThrowIfNull(loan);

        EnsureLoaded();

        if (_loans.ContainsKey(loan.Id))
        {
            throw new InvalidOperationException(
                $"Видача з id={loan.Id} уже існує.");
        }

        _loans.Add(
            loan.Id,
            loan);

        Flush();
    }

    public void UpdateLoan(Loan loan)
    {
        ArgumentNullException.ThrowIfNull(loan);

        EnsureLoaded();

        _loans[loan.Id] = loan;

        Flush();
    }
}