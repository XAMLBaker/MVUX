using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Mvux.Core.Internal;

internal sealed class StateImpl<T> : IState<T>
{
    private Option<T> _current;
    private readonly List<Channel<Message<T>>> _subscribers = [];
    private readonly SemaphoreSlim _lock = new(1, 1);

    public StateImpl(Option<T> initial) => _current = initial;

    public async IAsyncEnumerable<Message<T>> GetSource(
        [EnumeratorCancellation] CancellationToken ct)
    {
        var channel = Channel.CreateUnbounded<Message<T>>();

        await _lock.WaitAsync(ct);
        try
        {
            _subscribers.Add(channel);
            channel.Writer.TryWrite(CurrentMessage());
        }
        finally
        {
            _lock.Release();
        }

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
         : Message<T>.Initial;

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
            foreach (var sub in _subscribers)
                sub.Writer.TryWrite(CurrentMessage());
        }
        finally
        {
            _lock.Release();
        }
    }
}
