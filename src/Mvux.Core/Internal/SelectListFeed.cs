using System.Runtime.CompilerServices;

namespace Mvux.Core.Internal;

/// <summary>Transforms each item in the list. Preserves IListFeed type.</summary>
internal sealed class SelectListFeed<T, TResult>(
    IListFeed<T> source,
    Func<T, TResult> selector) : IListFeed<TResult>
{
    public async IAsyncEnumerable<Message<IReadOnlyList<TResult>>> GetSource(
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var msg in source.GetSource(ct))
        {
            if (msg.IsLoading && !msg.HasData)
                yield return Message<IReadOnlyList<TResult>>.Loading();
            else if (msg.Error != null)
                yield return Message<IReadOnlyList<TResult>>.Errored(msg.Error);
            else if (msg.Data.IsSome(out var list))
                yield return Message<IReadOnlyList<TResult>>.WithData(
                    list.Select(selector).ToList(), msg.IsLoading);
            else
                yield return Message<IReadOnlyList<TResult>>.None();
        }
    }
}

/// <summary>Filters items within each list. Preserves IListFeed type.</summary>
internal sealed class WhereListFeed<T>(
    IListFeed<T> source,
    Func<T, bool> predicate) : IListFeed<T>
{
    public async IAsyncEnumerable<Message<IReadOnlyList<T>>> GetSource(
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var msg in source.GetSource(ct))
        {
            if (msg.IsLoading && !msg.HasData)
                yield return Message<IReadOnlyList<T>>.Loading();
            else if (msg.Error != null)
                yield return Message<IReadOnlyList<T>>.Errored(msg.Error);
            else if (msg.Data.IsSome(out var list))
                yield return Message<IReadOnlyList<T>>.WithData(
                    list.Where(predicate).ToList(), msg.IsLoading);
            else
                yield return Message<IReadOnlyList<T>>.None();
        }
    }
}
