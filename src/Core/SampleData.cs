using Core.Domain;

namespace Core;

public static class SampleData
{
    public static IReadOnlyList<BookCopy> Copies() =>
    [
        BookCopy.Create(
            "B-001",
            "9786171292507",
            "Кобзар",
            1840,
            "Тарас Шевченко"),

        BookCopy.Create(
            "B-002",
            "9789660389887",
            "Лісова пісня",
            1911,
            "Леся Українка"),

        BookCopy.Create(
            "B-003",
            "9786175851748",
            "Тигролови",
            1944,
            "Іван Багряний"),

        BookCopy.Create(
            "B-004",
            "9786171290992",
            "Місто",
            1928,
            "Валер'ян Підмогильний"),

        BookCopy.Create(
            "B-005",
            "9789661033543",
            "Захар Беркут",
            1883,
            "Іван Франко"),

        BookCopy.Create(
            "B-006",
            "9786170000001",
            "Кайдашева сім'я",
            1879,
            "Іван Нечуй-Левицький"),

        BookCopy.Create(
            "B-007",
            "9786170000002",
            "Тіні забутих предків",
            1911,
            "Михайло Коцюбинський"),

        BookCopy.Create(
            "B-008",
            "9786170000003",
            "Чорна рада",
            1857,
            "Пантелеймон Куліш"),

        BookCopy.Create(
            "B-009",
            "9786170000004",
            "Маруся Чурай",
            1979,
            "Ліна Костенко"),

        BookCopy.Create(
            "B-010",
            "9786170000005",
            "Сад Гетсиманський",
            1950,
            "Іван Багряний"),

        BookCopy.Create(
            "B-011",
            "9786170000006",
            "Україна в огні",
            1943,
            "Олександр Довженко"),

        BookCopy.Create(
            "B-012",
            "9786170000007",
            "Intermezzo",
            1908,
            "Михайло Коцюбинський"),

        BookCopy.Create(
            "B-013",
            "9786170000008",
            "Енеїда",
            1798,
            "Іван Котляревський"),

        BookCopy.Create(
            "B-014",
            "9786170000009",
            "Хіба ревуть воли",
            1880,
            "Панас Мирний"),

        BookCopy.Create(
            "B-015",
            "9786170000010",
            "Я (Романтика)",
            1924,
            "Микола Хвильовий"),

        BookCopy.Create(
            "B-016",
            "9786171292507",
            "Кобзар",
            1840,
            "Тарас Шевченко"),

        BookCopy.Create(
            "B-017",
            "9786171292507",
            "Кобзар",
            1840,
            "Тарас Шевченко"),

        BookCopy.Create(
            "B-018",
            "9786175851748",
            "Тигролови",
            1944,
            "Іван Багряний")
    ];

    public static IReadOnlyList<Loan> Loans(
        IReadOnlyList<BookCopy> copies)
    {
        ArgumentNullException.ThrowIfNull(copies);

        var byId = copies.ToDictionary(
            copy => copy.Id,
            StringComparer.OrdinalIgnoreCase);

        var loans = new List<Loan>();
        DateTime today = DateTime.Today;

        void OpenLoan(
            string loanId,
            string copyId,
            string readerId,
            int issuedDaysAgo)
        {
            BookCopy copy = byId[copyId];
            loans.Add(Loan.Open(
                loanId,
                copy.Id,
                readerId,
                today.AddDays(-issuedDaysAgo)));
            copy.Issue();
        }

        void CloseLoan(
            string loanId,
            string copyId,
            string readerId,
            int issuedDaysAgo,
            int returnedDaysAgo)
        {
            BookCopy copy = byId[copyId];
            Loan loan = Loan.Open(
                loanId,
                copy.Id,
                readerId,
                today.AddDays(-issuedDaysAgo));
            loan.Close(today.AddDays(-returnedDaysAgo));
            loans.Add(loan);
        }

        OpenLoan("LN-01", "B-001", "R-001", 30);
        OpenLoan("LN-02", "B-002", "R-002", 21);
        OpenLoan("LN-03", "B-003", "R-003", 4);

        CloseLoan("LN-04", "B-004", "R-001", 80, 60);
        CloseLoan("LN-05", "B-005", "R-001", 70, 50);
        CloseLoan("LN-06", "B-006", "R-001", 60, 40);
        CloseLoan("LN-07", "B-007", "R-001", 50, 30);

        CloseLoan("LN-08", "B-008", "R-002", 45, 25);
        CloseLoan("LN-09", "B-009", "R-002", 40, 20);

        CloseLoan("LN-10", "B-010", "R-003", 35, 18);
        CloseLoan("LN-11", "B-011", "R-003", 32, 16);

        CloseLoan("LN-12", "B-012", "R-004", 28, 12);
        CloseLoan("LN-13", "B-013", "R-004", 26, 10);

        CloseLoan("LN-14", "B-014", "R-005", 24, 9);
        CloseLoan("LN-15", "B-015", "R-005", 22, 8);

        CloseLoan("LN-16", "B-016", "R-006", 20, 6);

        return loans;
    }
}