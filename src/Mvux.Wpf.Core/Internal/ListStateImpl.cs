using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Mvux.Wpf.Core.Internal;

internal sealed class ListStateImpl<T> : IListState<T>
{
    private ImmutableList<T> _items;
    private readonly List<Channel<Message<IReadOnlyList<T>>>> _subscribers = [];
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ListStateImpl(IEnumerable<T>? initial = null)
        => _items = initial is null ? ImmutableList<T>.Empty : [..initial];

    public async IAsyncEnumerable<Message<IReadOnlyList<T>>> GetSource(
        [EnumeratorCancellation] CancellationToken ct)
    {
        var channel = Channel.CreateUnbounded<Message<IReadOnlyList<T>>>();

        await _lock.WaitAsync(ct);
        try
        {
            _subscribers.Add(channel);
            channel.Writer.TryWrite(Message<IReadOnlyList<T>>.WithData(_items));
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

    public ValueTask AddAsync(T item, CancellationToken ct = default)
        => MutateAsync(list => list.Add(item), ct);

    public ValueTask AddRangeAsync(IEnumerable<T> items, CancellationToken ct = default)
        => MutateAsync(list => list.AddRange(items), ct);

    public ValueTask InsertAtAsync(int index, T item, CancellationToken ct = default)
        => MutateAsync(list => list.Insert(index, item), ct);

    public ValueTask RemoveAsync(Func<T, bool> predicate, CancellationToken ct = default)
        => MutateAsync(list => list.RemoveAll(x => predicate(x)), ct);

    public ValueTask RemoveAsync(T item, CancellationToken ct = default)
        => MutateAsync(list => list.Remove(item), ct);

    public async ValueTask UpdateAsync(Func<IReadOnlyList<T>, IReadOnlyList<T>> updater, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _items = [..updater(_items)];
            Broadcast();
        }
        finally
        {
            _lock.Release();
        }
    }

    public ValueTask UpdateAsync(T oldItem, T newItem, CancellationToken ct = default)
        => MutateAsync(list =>
        {
            var idx = list.IndexOf(oldItem);
            return idx >= 0 ? list.SetItem(idx, newItem) : list;
        }, ct);

    public ValueTask SetAsync(IEnumerable<T> items, CancellationToken ct = default)
        => MutateAsync(_ => ImmutableList.CreateRange(items), ct);

    public ValueTask ClearAsync(CancellationToken ct = default)
        => MutateAsync(list => list.Clear(), ct);

    private async ValueTask MutateAsync(Func<ImmutableList<T>, ImmutableList<T>> mutate, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _items = mutate(_items);
            Broadcast();
        }
        finally
        {
            _lock.Release();
        }
    }

    private void Broadcast()
    {
        var msg = Message<IReadOnlyList<T>>.WithData(_items);
        foreach (var sub in _subscribers)
            sub.Writer.TryWrite(msg);
    }
}
