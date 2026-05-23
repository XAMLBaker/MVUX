namespace Lw.Mvux;

public enum OptionType { Undefined, None, Some }

public readonly struct Option<T>
{
    private readonly OptionType _type;
    private readonly T? _value;

    private Option(OptionType type, T? value = default)
    {
        _type = type;
        _value = value;
    }

    public static Option<T> Undefined() => new(OptionType.Undefined);
    public static Option<T> None() => new(OptionType.None);
    public static Option<T> Some(T value) => new(OptionType.Some, value);

    public OptionType Type => _type;
    public bool IsUndefined => _type == OptionType.Undefined;
    public bool IsNone => _type == OptionType.None;

    public bool IsSome(out T value)
    {
        value = _value!;
        return _type == OptionType.Some;
    }

    // Backward compat
    public bool HasValue => _type == OptionType.Some;
    public T Value => HasValue ? _value! : throw new InvalidOperationException("Option has no value.");

    public bool TryGetValue(out T value)
    {
        value = _value!;
        return HasValue;
    }

    public T? SomeOrDefault() => _value;

    public Option<TResult> Select<TResult>(Func<T, TResult> selector)
        => _type switch
        {
            OptionType.Some => Option<TResult>.Some(selector(_value!)),
            OptionType.None => Option<TResult>.None(),
            _ => Option<TResult>.Undefined(),
        };

    public override string ToString()
        => _type switch
        {
            OptionType.Undefined => $"Undefined<{typeof(T).Name}>",
            OptionType.None => $"None<{typeof(T).Name}>",
            _ => $"Some({_value})",
        };
}
