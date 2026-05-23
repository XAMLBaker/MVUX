using System.Runtime.CompilerServices;

namespace Mvux.Core.Internal;

internal sealed class AsyncListFeed<T>(Func<CancellationToken, Task<IReadOnlyList<T>>> fetch) : IListFeed<T>
{
    public async IAsyncEnumerable<Message<IReadOnlyList<T>>> GetSource(
        [EnumeratorCancellation] CancellationToken ct)
    {
        yield return Message<IReadOnlyList<T>>.Loading();

        Message<IReadOnlyList<T>> result;
        try
        {
            var items = await fetch(ct);
            result = Message<IReadOnlyList<T>>.WithData(items);
        }
        catch (OperationCanceledException)
        {
            yield break;
        }
        catch (Exception ex)
        {
            result = Message<IReadOnlyList<T>>.Errored(ex);
        }

        yield return result;
    }
}

internal sealed class AsyncEnumerableListFeed<T>(
    Func<CancellationToken, IAsyncEnumerable<IReadOnlyList<T>>> source) : IListFeed<T>
{
    public async IAsyncEnumerable<Message<IReadOnlyList<T>>> GetSource(
        [EnumeratorCancellation] CancellationToken ct)
    {
        yield return Message<IReadOnlyList<T>>.Loading();
        await foreach (var items in source(ct))
            yield return Message<IReadOnlyList<T>>.WithData(items);
    }
}
