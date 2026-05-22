using System.Runtime.CompilerServices;

namespace Mvux.Wpf.Core;

public interface IFeed
{
    IAsyncEnumerable<IMessage> GetMessages(CancellationToken ct);
}

public interface IFeed<T> : IFeed
{
    IAsyncEnumerable<Message<T>> GetSource(CancellationToken ct);

    async IAsyncEnumerable<IMessage> IFeed.GetMessages([EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var msg in GetSource(ct))
            yield return msg;
    }
}
