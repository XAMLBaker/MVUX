using System.Runtime.CompilerServices;

namespace Luke.Mvux.Internal;

internal sealed class ValueListFeed<T>(IReadOnlyList<T> items) : IListFeed<T>
{
    public async IAsyncEnumerable<Message<IReadOnlyList<T>>> GetSource(
        [EnumeratorCancellation] CancellationToken ct)
    {
        await Task.Yield();
        ct.ThrowIfCancellationRequested();
        yield return Message<IReadOnlyList<T>>.WithData(items);
    }
}
