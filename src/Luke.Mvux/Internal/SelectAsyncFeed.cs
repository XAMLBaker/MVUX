using System.Runtime.CompilerServices;

namespace Luke.Mvux.Internal;

internal sealed class SelectAsyncFeed<T, TResult>(
    IFeed<T> source,
    Func<T, CancellationToken, ValueTask<TResult>> selector) : IFeed<TResult>
{
    public async IAsyncEnumerable<Message<TResult>> GetSource(
        [EnumeratorCancellation] CancellationToken ct)
    {
        var hasLastResult = false;
        TResult? lastResult = default;

        await foreach (var msg in source.GetSource(ct))
        {
            if (msg.IsLoading && !msg.HasData)
            {
                yield return hasLastResult
                    ? Message<TResult>.WithData(lastResult!, isLoading: true)
                    : Message<TResult>.Loading();
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

            yield return hasLastResult
                ? Message<TResult>.WithData(lastResult!, isLoading: true)
                : Message<TResult>.Loading();

            var (result, error) = await InvokeAsync(msg.Data.SomeOrDefault()!, ct);
            if (error is OperationCanceledException)
                yield break;
            if (error != null)
                yield return Message<TResult>.Errored(error);
            else
            {
                lastResult = result!;
                hasLastResult = true;
                yield return Message<TResult>.WithData(lastResult);
            }
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
