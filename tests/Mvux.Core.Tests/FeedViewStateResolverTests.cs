using Luke.Mvux;

namespace Mvux.Core.Tests;

public class FeedViewStateResolverTests
{
    [Fact]
    public void Resolve_Data_Message_Returns_Data()
    {
        var message = Message<string>.WithData("Seoul");

        var state = FeedViewStateResolver.Resolve(message);

        Assert.Equal(FeedViewStateKind.Data, state);
    }

    [Fact]
    public void Resolve_Data_And_Loading_Returns_Data()
    {
        var message = Message<string>.WithData("Seoul", isLoading: true);

        var state = FeedViewStateResolver.Resolve(message);

        Assert.Equal(FeedViewStateKind.Data, state);
    }

    [Fact]
    public void Resolve_Error_Without_Data_Returns_Error()
    {
        var message = Message<string>.Errored(new InvalidOperationException("boom"));

        var state = FeedViewStateResolver.Resolve(message);

        Assert.Equal(FeedViewStateKind.Error, state);
    }

    [Fact]
    public void Resolve_Loading_Without_Data_Returns_Loading()
    {
        var message = Message<string>.Loading();

        var state = FeedViewStateResolver.Resolve(message);

        Assert.Equal(FeedViewStateKind.Loading, state);
    }

    [Fact]
    public void Resolve_None_Returns_None()
    {
        var message = Message<string>.None();

        var state = FeedViewStateResolver.Resolve(message);

        Assert.Equal(FeedViewStateKind.None, state);
    }

    [Fact]
    public void Resolve_Data_Has_Priority_Over_Error_And_Loading()
    {
        var message = new FakeMessage(hasData: true, isLoading: true, error: new Exception("x"));

        var state = FeedViewStateResolver.Resolve(message);

        Assert.Equal(FeedViewStateKind.Data, state);
    }

    [Fact]
    public void Resolve_Error_Has_Priority_Over_Loading_When_No_Data()
    {
        var message = new FakeMessage(hasData: false, isLoading: true, error: new Exception("x"));

        var state = FeedViewStateResolver.Resolve(message);

        Assert.Equal(FeedViewStateKind.Error, state);
    }

    private sealed class FakeMessage : IMessage
    {
        public FakeMessage(bool hasData, bool isLoading, Exception? error)
        {
            HasData = hasData;
            IsLoading = isLoading;
            Error = error;
        }

        public bool IsLoading { get; }
        public bool HasData { get; }
        public bool IsNone => !HasData && !IsLoading && Error is null;
        public bool IsUndefined => false;
        public Exception? Error { get; }
        public object? DataObject => HasData ? "data" : null;
    }
}
