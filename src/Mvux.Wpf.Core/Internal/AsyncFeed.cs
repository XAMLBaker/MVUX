using System.Runtime.CompilerServices;

namespace Mvux.Wpf.Core.Internal;

internal sealed class AsyncFeed<T>(Func<CancellationToken, ValueTask<T>> fetch) : IFeed<T>
{
    public async IAsyncEnumerable<Message<T>> GetSource(
        [EnumeratorCancellation] CancellationToken ct)
    {
        yield return Message<T>.Loading();

        Message<T> result;
        try
        {
            var value = await fetch(ct);
            result = Message<T>.WithData(value);
        }
        catch (OperationCanceledException)
        {
            yield break;
        }
        catch (Exception ex)
        {
            result = Message<T>.Errored(ex);
        }

        yield return result;
    }
}
