/// усі LINQ-запити звітів в одному місці.
using Core.Domain;
using Core.Services;

namespace Core.Reports;

/// <summary>
/// Звіти LINQ to Objects. Чисті функції: приймають IEnumerable (не сховище), повертають
/// IReadOnlyList із DTO. Нічого не друкують і не знають про консоль.
/// </summary>
public static class ReportService
{
    // 1) фільтр + сортування + проєкція: хто прострочив повернення
    public static IReadOnlyList<OverdueLine> OverdueLoans(
        IEnumerable<Reader> readers, DateOnly today, int loanPeriodDays = LendingService.LoanPeriodDays) =>
        readers
            .SelectMany(r => r.Loans.Where(l => !l.IsClosed),
                (r, l) => new { Reader = r, Loan = l, Due = l.IssuedOn.AddDays(loanPeriodDays) })
            .Where(x => x.Due < today)
            .OrderByDescending(x => today.DayNumber - x.Due.DayNumber)
            .ThenBy(x => x.Loan.Id, StringComparer.Ordinal)
            .Select(x => new OverdueLine(
                x.Reader.Id, x.Reader.FullName, x.Loan.CopyId,
                x.Loan.IssuedOn, x.Due, today.DayNumber - x.Due.DayNumber))
            .ToList();

    // 2) топ-N зі стабільним порядком: другий ключ — унікальний Id читача
    public static IReadOnlyList<ReaderLine> TopReaders(IEnumerable<Reader> readers, int n) =>
        readers
            .OrderByDescending(r => r.Loans.Count)
            .ThenBy(r => r.Id, StringComparer.Ordinal)
            .Take(n)
            .Select(r => new ReaderLine(r.Id, r.FullName, r.Loans.Count, r.OpenLoansCount))
            .ToList();

    // 3) групування з підрахунком: примірники за видавцем (префікс ISBN без останнього блоку)
    public static IReadOnlyList<GroupSummary> ByPublisher(IEnumerable<BookCopy> copies) =>
        copies
            .GroupBy(c => Publisher(c.Isbn), StringComparer.OrdinalIgnoreCase)
            .Select(g => new GroupSummary(g.Key, g.Count(), g.Count(c => c.IsIssued)))
            .OrderByDescending(s => s.Count)
            .ThenBy(s => s.Key, StringComparer.Ordinal)
            .ToList();

    // 4) агрегати з обробкою порожньої послідовності (Average і Max на порожній кидають виняток)
    public static (int Copies, int Issued, double AvgCopiesPerTitle, int MaxCopiesPerTitle) Totals(
        IEnumerable<BookCopy> copies)
    {
        List<BookCopy> list = copies.ToList();
        if (list.Count == 0)
            return (0, 0, 0d, 0);

        List<int> perTitle = list
            .GroupBy(c => c.Isbn, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Count())
            .ToList();

        return (list.Count, list.Count(c => c.IsIssued), perTitle.Average(), perTitle.Max());
    }

    /// <summary>
    /// Лише для демо відкладеного виконання: НАВМИСНО повертає незавершений запит (IEnumerable),
    /// а не список. Запит виконується під час перебирання або виклику Count()/ToList(), і щоразу заново.
    /// Усі справжні звіти вище матеріалізуються через ToList().
    /// </summary>
    public static IEnumerable<BookCopy> IssuedQuery(IEnumerable<BookCopy> source) =>
        source.Where(c => c.IsIssued);

    private static string Publisher(string isbn)
    {
        int cut = isbn.LastIndexOf('-');
        return cut > 0 ? isbn[..cut] : isbn;
    }
}
