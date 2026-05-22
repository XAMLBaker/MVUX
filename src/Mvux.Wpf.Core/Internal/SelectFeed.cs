using System.Runtime.CompilerServices;

namespace Mvux.Wpf.Core.Internal;

internal sealed class SelectFeed<T, TResult>(IFeed<T> source, Func<T, TResult> selector) : IFeed<TResult>
{
    public async IAsyncEnumerable<Message<TResult>> GetSource(
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var msg in source.GetSource(ct))
        {
            if (msg.IsLoading && !msg.HasData)
                yield return Message<TResult>.Loading();
            else if (msg.Error != null)
                yield return Message<TResult>.Errored(msg.Error);
            else if (msg.HasData)
                yield return Message<TResult>.WithData(selector(msg.Data.SomeOrDefault()!), msg.IsLoading);
            else if (msg.IsNone)
                yield return Message<TResult>.None();
            else
                yield return Message<TResult>.Initial;
        }
    }
}

internal sealed class WhereFeed<T>(IFeed<T> source, Func<T, bool> predicate) : IFeed<T>
{
    public async IAsyncEnumerable<Message<T>> GetSource(
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var msg in source.GetSource(ct))
        {
            if (msg.IsLoading && !msg.HasData)
                yield return Message<T>.Loading();
            else if (msg.Error != null)
                yield return Message<T>.Errored(msg.Error);
            else if (msg.HasData)
                yield return predicate(msg.Data.SomeOrDefault()!) ? msg : Message<T>.None();
            else
                yield return Message<T>.None();
        }
    }
}
