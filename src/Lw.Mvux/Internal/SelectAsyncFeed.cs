using System.Runtime.CompilerServices;

namespace Lw.Mvux.Internal;

internal sealed class SelectAsyncFeed<T, TResult>(
    IFeed<T> source,
    Func<T, CancellationToken, ValueTask<TResult>> selector) : IFeed<TResult>
{
    public async IAsyncEnumerable<Message<TResult>> GetSource(
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var msg in source.GetSource(ct))
        {
            if (msg.IsLoading && !msg.HasData)
            {
                yield return Message<TResult>.Loading();
                continue;
            }
            if (msg.Error != null)
            {
                yield return Message<TResult>.Errored(msg.Error);
                continue;
            }
            if (!msg.HasData)
            {
                yield return Message<TResult>.None();
                continue;
            }

            yield return Message<TResult>.Loading();

            var (result, error) = await InvokeAsync(msg.Data.SomeOrDefault()!, ct);
            if (error is OperationCanceledException)
                yield break;
            if (error != null)
                yield return Message<TResult>.Errored(error);
            else
                yield return Message<TResult>.WithData(result!);
        }
    }

    private async ValueTask<(TResult?, Exception?)> InvokeAsync(T value, CancellationToken ct)
    {
        try
        {
            return (await selector(value, ct), null);
        }
        catch (Exception ex)
        {
            return (default, ex);
        }
    }
}
