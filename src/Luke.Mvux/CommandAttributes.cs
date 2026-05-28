namespace Luke.Mvux;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ImplicitCommandsAttribute(bool isEnabled = true) : Attribute
{
    public bool IsEnabled { get; } = isEnabled;
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class CommandAttribute(bool isEnabled = true) : Attribute
{
    public bool IsEnabled { get; } = isEnabled;
}

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ImplicitFeedCommandParameterAttribute(bool isEnabled = true) : Attribute
{
    public bool IsEnabled { get; } = isEnabled;
}

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FeedParameterAttribute(string feedPropertyName) : Attribute
{
    public string FeedPropertyName { get; } = feedPropertyName;
}
