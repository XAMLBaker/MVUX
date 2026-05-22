using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Mvux.Wpf.Core;

public sealed class Signal
{
    private readonly List<Channel<bool>> _listeners = [];
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async ValueTask Raise(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            foreach (var ch in _listeners)
                ch.Writer.TryWrite(true);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async IAsyncEnumerable<bool> Listen(
        [EnumeratorCancellation] CancellationToken ct)
    {
        var channel = Channel.CreateUnbounded<bool>();

        await _lock.WaitAsync(ct);
        try { _listeners.Add(channel); }
        finally { _lock.Release(); }

        try
        {
            await foreach (var v in channel.Reader.ReadAllAsync(ct))
                yield return v;
        }
        finally
        {
            await _lock.WaitAsync(CancellationToken.None);
            try { _listeners.Remove(channel); }
            finally { _lock.Release(); }
        }
    }
}
