namespace Argos.Entities;

/// <summary>Página de resultados: <c>{"data", "total", "page", "size"}</c>.</summary>
public sealed class PagedResult<T>(IReadOnlyList<T> data, int total, int page, int size)
{
    public IReadOnlyList<T> Data { get; } = data;
    public int Total { get; } = total;
    public int Page { get; } = page;
    public int Size { get; } = size;
}
