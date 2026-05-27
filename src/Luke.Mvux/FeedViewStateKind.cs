namespace Luke.Mvux;

public enum FeedViewStateKind
{
    None,
    Loading,
    Error,
    Data
}

public static class FeedViewStateResolver
{
    public static FeedViewStateKind Resolve(IMessage message)
    {
        if (message.HasData)
            return FeedViewStateKind.Data;

        if (message.Error is not null)
            return FeedViewStateKind.Error;

        if (message.IsLoading)
            return FeedViewStateKind.Loading;

        return FeedViewStateKind.None;
    }
}
