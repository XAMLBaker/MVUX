namespace Mvux.Core;

public readonly struct Message<T> : IMessage
{
    private Message(Option<T> data, bool isLoading, Exception? error)
    {
        Data = data;
        IsLoading = isLoading;
        Error = error;
    }

    /// <summary>Data axis — independent from IsLoading/Error.</summary>
    public Option<T> Data { get; }
    public bool IsLoading { get; }
    public Exception? Error { get; }

    public bool HasData => Data.IsSome(out _);
    public bool IsNone => Data.IsNone;
    public bool IsUndefined => Data.IsUndefined;

    object? IMessage.DataObject => Data.IsSome(out var v) ? (object?)v : null;

    /// <summary>Initial state before any data has arrived (Undefined + loading).</summary>
    public static Message<T> Initial { get; } = new(Option<T>.Undefined(), isLoading: true, error: null);

    public static Message<T> Loading() => new(Option<T>.Undefined(), isLoading: true, error: null);

    /// <summary>Creates a message carrying data. Pass isLoading=true to represent refreshing stale data.</summary>
    public static Message<T> WithData(T value, bool isLoading = false) => new(Option<T>.Some(value), isLoading, error: null);

    public static Message<T> Errored(Exception ex) => new(Option<T>.None(), isLoading: false, error: ex);

    public static Message<T> None() => new(Option<T>.None(), isLoading: false, error: null);
}
