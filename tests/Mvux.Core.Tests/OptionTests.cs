using Mvux.Core;

namespace Mvux.Core.Tests;

public class OptionTests
{
    [Fact]
    public void Some_HasValue_True()
    {
        var opt = Option<int>.Some(42);
        Assert.True(opt.HasValue);
        Assert.Equal(42, opt.Value);
    }

    [Fact]
    public void None_HasValue_False()
    {
        var opt = Option<int>.None();
        Assert.False(opt.HasValue);
        Assert.Throws<InvalidOperationException>(() => opt.Value);
    }

    [Fact]
    public void Select_Some_TransformsValue()
    {
        var opt = Option<int>.Some(5).Select(x => x * 2);
        Assert.True(opt.HasValue);
        Assert.Equal(10, opt.Value);
    }

    [Fact]
    public void Select_None_StaysNone()
    {
        var opt = Option<int>.None().Select(x => x * 2);
        Assert.False(opt.HasValue);
    }
}
