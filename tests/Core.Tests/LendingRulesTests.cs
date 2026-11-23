using Core.Domain;
using Core.Exceptions;
using Core.Reports;
using Core.Services;
using Core.Storage;
using Core.Tests.TestData;

namespace Core.Tests;

public class LendingRulesTests
{
    [Fact]
    public void IssueCopy_WhenAlreadyIssued_ThrowsDomainExceptionAndKeepsState()
    {
        var store = new InMemoryLibraryStore();
        var service = new LendingService(store);
        BookCopy copy = service.AddBook("9786171001001", "Вже видана", 1995);
        service.IssueCopy(copy.Id, "R-001");

        int loansBefore = store.ListLoans().Count;
        int events = 0;
        service.Changed += (_, _) => events++;

        DomainException ex = Assert.Throws<DomainException>(
            () => service.IssueCopy(copy.Id, "R-002"));

        Assert.Contains("вже виданий", ex.Message);
        Assert.Equal(loansBefore, store.ListLoans().Count);
        Assert.True(store.GetCopyById(copy.Id)!.IsIssued);
        Assert.Equal(0, events);
    }

    [Fact]
    public void AddBook_RaisesChangedExactlyOnce()
    {
        var service = new LendingService(new InMemoryLibraryStore());
        int events = 0;
        service.Changed += (_, _) => events++;

        service.AddBook("9786171001002", "Нова книга", 2004);

        Assert.Equal(1, events);
    }

    [Fact]
    public void ReturnCopy_WhenCopyWasNotIssued_ThrowsAndKeepsState()
    {
        var store = new InMemoryLibraryStore();
        var service = new LendingService(store);
        BookCopy copy = service.AddBook("9786171001003", "Невидана", 1998);
        Loan loan = Loan.Open("LN-X", copy.Id, "R-1", new DateTime(2020, 1, 10));
        store.AddLoan(loan);

        int events = 0;
        service.Changed += (_, _) => events++;

        DomainException ex = Assert.Throws<DomainException>(
            () => service.ReturnCopy(loan.Id));

        Assert.Contains("не виданий", ex.Message);
        Assert.False(store.GetCopyById(copy.Id)!.IsIssued);
        Assert.Equal(LoanStatus.Open, store.GetLoanById(loan.Id)!.Status);
        Assert.Equal(0, events);
    }

    [Fact]
    public void Create_EmptyIsbn_ThrowsArgumentException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => BookCopy.Create("id-1", " ", "Назва", 2000));

        Assert.Equal("isbn", ex.ParamName);
    }

    [Fact]
    public void IssueCopy_SixthOpenLoan_ThrowsDomainException()
    {
        var store = new InMemoryLibraryStore();
        var service = new LendingService(store);

        for (int i = 0; i < 5; i++)
        {
            BookCopy copy = service.AddBook($"978617100110{i}", $"Книга {i}", 2000);
            service.IssueCopy(copy.Id, "R-LIMIT");
        }

        BookCopy extra = service.AddBook("9786171001199", "Зайва", 2000);
        int loansBefore = store.ListLoans().Count;

        Assert.Throws<DomainException>(() => service.IssueCopy(extra.Id, "R-LIMIT"));

        Assert.Equal(loansBefore, store.ListLoans().Count);
        Assert.False(store.GetCopyById(extra.Id)!.IsIssued);
    }

    [Theory]
    [InlineData(1840, 1840, 1911, 1840, 2)]
    [InlineData(2001, 2002, 2001, 2002, 1)]
    [InlineData(1990, 1991, 1992, 1980, 0)]
    public void ByYear_GroupsCopies(
        int year1,
        int year2,
        int year3,
        int queriedYear,
        int expectedCount)
    {
        BookCopy[] copies =
        [
            LibrarySamples.Copy("c1", year1),
            LibrarySamples.Copy("c2", year2),
            LibrarySamples.Copy("c3", year3),
        ];

        IReadOnlyList<YearGroupSummary> groups = ReportService.ByYear(copies);
        int[] years = groups.Select(group => group.Year).ToArray();

        Assert.Equal(years.OrderBy(year => year).ToArray(), years);

        YearGroupSummary? group = groups.SingleOrDefault(item => item.Year == queriedYear);
        Assert.Equal(expectedCount, group?.CopyCount ?? 0);
    }

    [Fact]
    public void TopReaders_OrdersByCountThenByReaderId()
    {
        DateTime issuedOn = new(2024, 3, 1);
        var loans = new List<Loan>
        {
            Loan.Open("1", "c", "R-2", issuedOn),
            Loan.Open("2", "c", "R-2", issuedOn),
            Loan.Open("3", "c", "R-1", issuedOn),
            Loan.Open("4", "c", "R-1", issuedOn),
            Loan.Open("5", "c", "R-3", issuedOn),
        };

        Loan cancelled = Loan.Open("6", "c", "R-3", issuedOn);
        cancelled.Cancel();
        loans.Add(cancelled);

        IReadOnlyList<TopReaderLine> top = ReportService.TopReaders(loans);

        Assert.Equal(["R-1", "R-2", "R-3"], top.Select(line => line.ReaderId));
        Assert.Equal([2, 2, 1], top.Select(line => line.LoanCount));
    }
}
