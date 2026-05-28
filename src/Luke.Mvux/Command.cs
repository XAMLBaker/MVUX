namespace Luke.Mvux;

public interface ICommandBuilder
{
    ICommandBuilder<T> Given<T>(IFeed<T> feed);
    void Then(Func<CancellationToken, ValueTask> execute);
    void Execute(Func<CancellationToken, ValueTask> execute) => Then(execute);
}

public interface ICommandBuilder<T>
{
    IConditionalCommandBuilder<T> When(Func<T, bool> canExecute);
    void Then(Func<T, CancellationToken, ValueTask> execute);
    void Execute(Func<T, CancellationToken, ValueTask> execute) => Then(execute);
}

public interface IConditionalCommandBuilder<T>
{
    void Then(Func<T, CancellationToken, ValueTask> execute);
    void Execute(Func<T, CancellationToken, ValueTask> execute) => Then(execute);
}

public static class Command
{
    public static IAsyncCommand Async(Func<CancellationToken, ValueTask> execute)
        => new AsyncCommand((_, ct) => execute(ct));

    public static IAsyncCommand Async<T>(Func<T, CancellationToken, ValueTask> execute)
        => new AsyncCommand(
            (p, ct) => p is T value ? execute(value, ct) : ValueTask.CompletedTask,
            p => p is T);

    public static IAsyncCommand Create(Action<ICommandBuilder> build)
    {
        var builder = new CommandBuilder();
        build(builder);
        return builder.Build();
    }

    public static IAsyncCommand Create<T>(Action<ICommandBuilder<T>> build)
    {
        var builder = new CommandBuilder<T>();
        build(builder);
        return builder.Build();
    }

    private sealed class CommandBuilder : ICommandBuilder
    {
        private Func<CancellationToken, ValueTask>? _execute;
        private Func<object?, CancellationToken, ValueTask>? _executeWithParam;
        private Func<object?, bool>? _canExecute;

        public ICommandBuilder<T> Given<T>(IFeed<T> feed)
            => new FeedCommandBuilder<T>(feed, this);

        public void Then(Func<CancellationToken, ValueTask> execute)
            => _execute = execute;

        public IAsyncCommand Build()
        {
            if (_executeWithParam is not null)
                return new AsyncCommand(_executeWithParam, _canExecute);

            var execute = _execute ?? (_ => ValueTask.CompletedTask);
            return new AsyncCommand((_, ct) => execute(ct));
        }

        private sealed class FeedCommandBuilder<T>(IFeed<T> feed, CommandBuilder owner) : ICommandBuilder<T>, IConditionalCommandBuilder<T>
        {
            private Func<T, bool>? _when;

            public IConditionalCommandBuilder<T> When(Func<T, bool> canExecute)
            {
                _when = canExecute;
                owner._canExecute = _ => true;
                return this;
            }

            public void Then(Func<T, CancellationToken, ValueTask> execute)
            {
                owner._executeWithParam = async (_, ct) =>
                {
                    var value = await feed.GetCurrentAsync(ct);
                    if (_when is not null && !_when(value))
                        return;
                    await execute(value, ct);
                };
            }
        }
    }

    private sealed class CommandBuilder<T> : ICommandBuilder<T>, IConditionalCommandBuilder<T>
    {
        private Func<T, bool>? _when;
        private Func<T, CancellationToken, ValueTask>? _execute;

        public IConditionalCommandBuilder<T> When(Func<T, bool> canExecute)
        {
            _when = canExecute;
            return this;
        }

        public void Then(Func<T, CancellationToken, ValueTask> execute)
            => _execute = execute;

        public IAsyncCommand Build()
        {
            var execute = _execute ?? ((_, _) => ValueTask.CompletedTask);
            return new AsyncCommand(
                async (parameter, ct) =>
                {
                    if (parameter is not T value)
                        return;
                    await execute(value, ct);
                },
                parameter => parameter is T value && (_when is null || _when(value)));
        }
    }
}
