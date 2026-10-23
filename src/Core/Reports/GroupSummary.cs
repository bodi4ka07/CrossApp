/// рядок групування: ключ, скільки примірників, скільки з них видано.
namespace Core.Reports;

/// <summary>Підсумок групи: скільки примірників у групі і скільки з них зараз видано.</summary>
public sealed record GroupSummary(string Key, int Count, int IssuedCount);
