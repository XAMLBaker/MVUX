using Luke.Mvux;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Luke.Mvux.Wpf;

public class FeedView : ContentControl
{
    private CancellationTokenSource _cts = new();
    private IMessage? _lastMessage;
    private readonly FeedViewState _state;

    // ── Dependency Properties ────────────────────────────────────────────────

    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(nameof(Source), typeof(IFeed), typeof(FeedView),
            new PropertyMetadata(null, OnSourceChanged));

    public static readonly DependencyProperty LoadingTemplateProperty =
        DependencyProperty.Register(nameof(LoadingTemplate), typeof(DataTemplate), typeof(FeedView),
            new PropertyMetadata(null, OnTemplateChanged));

    public static readonly DependencyProperty FeedDataTemplateProperty =
        DependencyProperty.Register(nameof(FeedDataTemplate), typeof(DataTemplate), typeof(FeedView),
            new PropertyMetadata(null, OnTemplateChanged));

    public static readonly DependencyProperty ErrorTemplateProperty =
        DependencyProperty.Register(nameof(ErrorTemplate), typeof(DataTemplate), typeof(FeedView),
            new PropertyMetadata(null, OnTemplateChanged));

    public static readonly DependencyProperty NoneTemplateProperty =
        DependencyProperty.Register(nameof(NoneTemplate), typeof(DataTemplate), typeof(FeedView),
            new PropertyMetadata(null, OnTemplateChanged));

    public IFeed? Source
    {
        get => (IFeed?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public DataTemplate? LoadingTemplate
    {
        get => (DataTemplate?)GetValue(LoadingTemplateProperty);
        set => SetValue(LoadingTemplateProperty, value);
    }

    public DataTemplate? FeedDataTemplate
    {
        get => (DataTemplate?)GetValue(FeedDataTemplateProperty);
        set => SetValue(FeedDataTemplateProperty, value);
    }

    public DataTemplate? ErrorTemplate
    {
        get => (DataTemplate?)GetValue(ErrorTemplateProperty);
        set => SetValue(ErrorTemplateProperty, value);
    }

    public DataTemplate? NoneTemplate
    {
        get => (DataTemplate?)GetValue(NoneTemplateProperty);
        set => SetValue(NoneTemplateProperty, value);
    }

    // ── Public ───────────────────────────────────────────────────────────────

    public ICommand RefreshCommand => _state.Refresh!;

    // ── Default templates ────────────────────────────────────────────────────

    private static readonly DataTemplate DefaultLoadingTemplate = BuildDefaultLoadingTemplate();
    private static readonly DataTemplate DefaultErrorTemplate = BuildDefaultErrorTemplate();

    private static DataTemplate BuildDefaultLoadingTemplate()
    {
        var factory = new FrameworkElementFactory(typeof(TextBlock));
        factory.SetValue(TextBlock.TextProperty, "Loading...");
        factory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        return new DataTemplate { VisualTree = factory };
    }

    private static DataTemplate BuildDefaultErrorTemplate()
    {
        var factory = new FrameworkElementFactory(typeof(TextBlock));
        factory.SetBinding(TextBlock.TextProperty, new Binding("Error.Message"));
        factory.SetValue(TextBlock.ForegroundProperty, Brushes.Red);
        factory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
        return new DataTemplate { VisualTree = factory };
    }

    // ── Constructor ──────────────────────────────────────────────────────────

    public FeedView()
    {
        _state = new FeedViewState { Refresh = new RelayCommand(Restart) };
        Content = _state;
        DataContextChanged += (_, e) => _state.Parent = e.NewValue;
        Unloaded += (_, _) => _cts.Cancel();
        Loaded += (_, _) => Restart();
    }

    // ── Change handlers ──────────────────────────────────────────────────────

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var fv = (FeedView)d;
        if (fv.IsLoaded) fv.Restart();
    }

    private static void OnTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var fv = (FeedView)d;
        if (fv._lastMessage is { } msg) fv.ApplyMessage(msg);
        else fv.ApplyNone();
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
                Dispatcher.Invoke(() =>
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

        switch (FeedViewStateResolver.Resolve(msg))
        {
            case FeedViewStateKind.Data:
                _state.Data = msg.DataObject;
                _state.Error = msg.Error;
                ContentTemplate = FeedDataTemplate;
                return;

            case FeedViewStateKind.Error:
                _state.Data = null;
                _state.Error = msg.Error;
                ContentTemplate = ErrorTemplate ?? DefaultErrorTemplate;
                return;

            case FeedViewStateKind.Loading:
                _state.Data = null;
                _state.Error = null;
                ContentTemplate = LoadingTemplate ?? DefaultLoadingTemplate;
                return;

            default:
                ApplyNone();
                return;
        }
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
