using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Mvux.Wpf.Core.Internal;

internal sealed class AsyncState<T> : IState<T>
{
    private readonly Func<CancellationToken, ValueTask<T>> _fetch;
    private Option<T> _current = Option<T>.Undefined();
    private readonly List<Channel<Message<T>>> _subscribers = [];
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _loaded;

    public AsyncState(Func<CancellationToken, ValueTask<T>> fetch) => _fetch = fetch;

    public async IAsyncEnumerable<Message<T>> GetSource(
        [EnumeratorCancellation] CancellationToken ct)
    {
        var channel = Channel.CreateUnbounded<Message<T>>();

        await _lock.WaitAsync(ct);
        try
        {
            _subscribers.Add(channel);
            if (_loaded)
                channel.Writer.TryWrite(CurrentMessage());
            else
                channel.Writer.TryWrite(Message<T>.Loading());
        }
        finally
        {
            _lock.Release();
        }

        if (!_loaded)
            _ = LoadAsync(ct);

        try
        {
            await foreach (var msg in channel.Reader.ReadAllAsync(ct))
                yield return msg;
        }
        finally
        {
            await _lock.WaitAsync(CancellationToken.None);
            try { _subscribers.Remove(channel); }
            finally { _lock.Release(); }
        }
    }

    private Message<T> CurrentMessage()
        => _current.IsSome(out var v) ? Message<T>.WithData(v)
         : _current.IsNone ? Message<T>.None()
         : Message<T>.Loading();

    private async Task LoadAsync(CancellationToken ct)
    {
        T result;
        try
        {
            result = await _fetch(ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            await BroadcastAsync(Message<T>.Errored(ex));
            return;
        }

        await _lock.WaitAsync(CancellationToken.None);
        try
        {
            _current = Option<T>.Some(result);
            _loaded = true;
        }
        finally
        {
            _lock.Release();
        }

        await BroadcastAsync(Message<T>.WithData(result));
    }

    public async ValueTask SetAsync(T value, CancellationToken ct = default)
        => await UpdateAsync(_ => Option<T>.Some(value), ct);

    public ValueTask UpdateAsync(Func<T?, T?> updater, CancellationToken ct = default)
        => UpdateAsync(opt =>
        {
            var result = updater(opt.SomeOrDefault());
            return result is null ? Option<T>.None() : Option<T>.Some(result!);
        }, ct);

    public async ValueTask UpdateAsync(Func<Option<T>, Option<T>> updater, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _current = updater(_current);
            _loaded = true;
            foreach (var sub in _subscribers)
                sub.Writer.TryWrite(CurrentMessage());
        }
        finally
        {
            _lock.Release();
        }
    }

    private async ValueTask BroadcastAsync(Message<T> msg)
    {
        await _lock.WaitAsync(CancellationToken.None);
        try
        {
            foreach (var sub in _subscribers)
                sub.Writer.TryWrite(msg);
        }
        finally
        {
            _lock.Release();
        }
    }
}
