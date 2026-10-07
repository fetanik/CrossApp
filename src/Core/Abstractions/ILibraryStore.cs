using Core.Domain;

namespace Core.Abstractions;

public interface ILibraryStore
{
    IReadOnlyList<BookCopy> ListCopies();

    BookCopy? GetCopyById(string id);

    void AddCopy(BookCopy copy);

    void UpdateCopy(BookCopy copy);


    IReadOnlyList<Loan> ListLoans();

    Loan? GetLoanById(string id);

    void AddLoan(Loan loan);

    void UpdateLoan(Loan loan);
}