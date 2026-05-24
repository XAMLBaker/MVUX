namespace Luke.Mvux;

public interface IMessage
{
    bool IsLoading { get; }
    bool HasData { get; }
    bool IsNone { get; }
    bool IsUndefined { get; }
    Exception? Error { get; }
    object? DataObject { get; }
}
