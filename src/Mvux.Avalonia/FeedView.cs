using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Mvux.Core;
using System.Windows.Input;

namespace Mvux.Avalonia;

/// <summary>
/// IFeed를 직접 구독하고 Loading / Data / Error / None 템플릿을 자동 전환.
/// 모든 템플릿의 DataContext는 <see cref="FeedViewState"/>입니다.
/// </summary>
public class FeedView : ContentControl
{
    private CancellationTokenSource _cts = new();
    private IMessage? _lastMessage;
    private readonly FeedViewState _state;

    // ── Styled Properties ────────────────────────────────────────────────────

    public static readonly StyledProperty<IFeed?> SourceProperty =
        AvaloniaProperty.Register<FeedView, IFeed?>(nameof(Source));

    public static readonly StyledProperty<IDataTemplate?> LoadingTemplateProperty =
        AvaloniaProperty.Register<FeedView, IDataTemplate?>(nameof(LoadingTemplate));

    public static readonly StyledProperty<IDataTemplate?> FeedDataTemplateProperty =
        AvaloniaProperty.Register<FeedView, IDataTemplate?>(nameof(FeedDataTemplate));

    public static readonly StyledProperty<IDataTemplate?> ErrorTemplateProperty =
        AvaloniaProperty.Register<FeedView, IDataTemplate?>(nameof(ErrorTemplate));

    public static readonly StyledProperty<IDataTemplate?> NoneTemplateProperty =
        AvaloniaProperty.Register<FeedView, IDataTemplate?>(nameof(NoneTemplate));

    public IFeed? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public IDataTemplate? LoadingTemplate
    {
        get => GetValue(LoadingTemplateProperty);
        set => SetValue(LoadingTemplateProperty, value);
    }

    public IDataTemplate? FeedDataTemplate
    {
        get => GetValue(FeedDataTemplateProperty);
        set => SetValue(FeedDataTemplateProperty, value);
    }

    public IDataTemplate? ErrorTemplate
    {
        get => GetValue(ErrorTemplateProperty);
        set => SetValue(ErrorTemplateProperty, value);
    }

    public IDataTemplate? NoneTemplate
    {
        get => GetValue(NoneTemplateProperty);
        set => SetValue(NoneTemplateProperty, value);
    }

    // ── Public ───────────────────────────────────────────────────────────────

    /// <summary>FeedView 바깥에서 바인딩: Command="{Binding RefreshCommand, ElementName=feedView}"</summary>
    public ICommand RefreshCommand => _state.Refresh!;

    // ── Default templates ────────────────────────────────────────────────────

    private static readonly IDataTemplate DefaultLoadingTemplate =
        new FuncDataTemplate<FeedViewState>((_, _) =>
            new TextBlock { Text = "Loading...", VerticalAlignment = VerticalAlignment.Center });

    private static readonly IDataTemplate DefaultErrorTemplate =
        new FuncDataTemplate<FeedViewState>((state, _) =>
            new TextBlock
            {
                Text = state?.Error?.Message,
                Foreground = Brushes.Red,
                TextWrapping = TextWrapping.Wrap
            });

    // ── Static constructor ────────────────────────────────────────────────────

    static FeedView()
    {
        SourceProperty.Changed.AddClassHandler<FeedView>((fv, _) => fv.Restart());
        LoadingTemplateProperty.Changed.AddClassHandler<FeedView>((fv, _) => fv.OnTemplateChanged());
        FeedDataTemplateProperty.Changed.AddClassHandler<FeedView>((fv, _) => fv.OnTemplateChanged());
        ErrorTemplateProperty.Changed.AddClassHandler<FeedView>((fv, _) => fv.OnTemplateChanged());
        NoneTemplateProperty.Changed.AddClassHandler<FeedView>((fv, _) => fv.OnTemplateChanged());
    }

    // ── Constructor ──────────────────────────────────────────────────────────

    public FeedView()
    {
        _state = new FeedViewState { Refresh = new RelayCommand(Restart) };
        Content = _state;
        DataContextChanged += (_, _) => _state.Parent = DataContext;
        Unloaded += (_, _) => _cts.Cancel();
        Loaded += (_, _) => Restart();
    }

    // ── Template changed ─────────────────────────────────────────────────────

    private void OnTemplateChanged()
    {
        if (_lastMessage is { } msg) ApplyMessage(msg);
        else ApplyNone();
    }

    // ── Subscription ─────────────────────────────────────────────────────────

    private void Restart()
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();
        _lastMessage = null;

        if (Source is { } feed)
            _ = SubscribeAsync(feed, _cts.Token);
        else
            ApplyNone();
    }

    private async Task SubscribeAsync(IFeed feed, CancellationToken ct)
    {
        try
        {
            await foreach (var msg in feed.GetMessages(ct))
            {
                if (ct.IsCancellationRequested) return;
                var captured = msg;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!ct.IsCancellationRequested)
                        ApplyMessage(captured);
                });
            }
        }
        catch (OperationCanceledException) { }
    }

    // ── State application ────────────────────────────────────────────────────

    private void ApplyMessage(IMessage msg)
    {
        _lastMessage = msg;
        _state.Progress = msg.IsLoading;

        if (msg.HasData)
        {
            _state.Data = msg.DataObject;
            _state.Error = msg.Error;
            ContentTemplate = FeedDataTemplate;
            return;
        }

        _state.Data = null;
        _state.Error = msg.Error;

        if (msg.Error is not null)
        {
            ContentTemplate = ErrorTemplate ?? DefaultErrorTemplate;
            return;
        }

        if (msg.IsLoading)
        {
            ContentTemplate = LoadingTemplate ?? DefaultLoadingTemplate;
            return;
        }

        ApplyNone();
    }

    private void ApplyNone()
    {
        _lastMessage = null;
        _state.Data = null;
        _state.Error = null;
        _state.Progress = false;
        ContentTemplate = NoneTemplate;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class RelayCommand(Action execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute();
    }
}
