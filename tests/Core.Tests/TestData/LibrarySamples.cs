using Core.Domain;

namespace Core.Tests.TestData;

internal static class LibrarySamples
{
    public static BookCopy Copy(string id, int year = 2000)
    {
        string digits = new string(id.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
        {
            digits = "1000000000";
        }

        string isbn = "978" + digits.PadLeft(10, '0')[^10..];
        return BookCopy.Create(id, isbn, "Книга " + id, year);
    }
}
