using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Threading;
using Luke.Mvux;

namespace Luke.Mvux.Avalonia;

public class FeedListView : ContentControl
{
    private readonly ListBox _listBox;
    private readonly ContentPresenter _stateLayer;
    private ISelectionFeed? _selFeed;
    private CancellationTokenSource _selCts = new();
    private bool _syncingSelection;

    // ── Styled Properties ─────────────────────────────────────────────────────

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<FeedListView, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<FeedListView, IDataTemplate?>(nameof(ItemTemplate));

    public static readonly StyledProperty<IDataTemplate?> LoadingTemplateProperty =
        AvaloniaProperty.Register<FeedListView, IDataTemplate?>(nameof(LoadingTemplate));

    public static readonly StyledProperty<IDataTemplate?> ErrorTemplateProperty =
        AvaloniaProperty.Register<FeedListView, IDataTemplate?>(nameof(ErrorTemplate));

    public static readonly StyledProperty<IDataTemplate?> NoneTemplateProperty =
        AvaloniaProperty.Register<FeedListView, IDataTemplate?>(nameof(NoneTemplate));

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public IDataTemplate? LoadingTemplate
    {
        get => GetValue(LoadingTemplateProperty);
        set => SetValue(LoadingTemplateProperty, value);
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

    // ── Static constructor ────────────────────────────────────────────────────

    static FeedListView()
    {
        ItemsSourceProperty.Changed
            .AddClassHandler<FeedListView>((fv, e) => fv.ApplyItemsSource(e.NewValue as IEnumerable));

        ItemTemplateProperty.Changed
            .AddClassHandler<FeedListView>((fv, e) => fv._listBox.ItemTemplate = e.NewValue as IDataTemplate);
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    public FeedListView()
    {
        _listBox = new ListBox { IsVisible = false };
        _listBox.SelectionChanged += OnSelectionChanged;

        _stateLayer = new ContentPresenter();

        var root = new Grid();
        root.Children.Add(_stateLayer);
        root.Children.Add(_listBox);
        Content = root;
    }

    // ── ItemsSource 변경 ──────────────────────────────────────────────────────

    private void ApplyItemsSource(IEnumerable? source)
    {
        _selCts.Cancel();
        _selCts = new CancellationTokenSource();
        _selFeed = null;

        _listBox.ItemsSource = source;

        if (source is ISelectionFeed { HasSelection: true } selFeed)
        {
            _selFeed = selFeed;
            _ = SubscribeSelectionAsync(selFeed, _selCts.Token);
        }

        if (source == null)
            ShowNone();
        else
            _listBox.IsVisible = true;
    }

    private async Task SubscribeSelectionAsync(ISelectionFeed selFeed, CancellationToken ct)
    {
        try
        {
            await foreach (var msg in selFeed.GetSelectionMessages(ct))
            {
                if (ct.IsCancellationRequested) return;
                var captured = msg;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (ct.IsCancellationRequested) return;
                    _syncingSelection = true;
                    SelectionInterop.ApplySelection(_listBox, selFeed, captured.HasData ? captured.DataObject : null);
                    _syncingSelection = false;
                });
            }
        }
        catch (OperationCanceledException) { }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_syncingSelection || _selFeed == null) return;
        _ = _selFeed.SetSelectedAsync(SelectionInterop.ReadSelection(_listBox, _selFeed), _selCts.Token);
    }

    // ── State display ─────────────────────────────────────────────────────────

    private void ShowNone()
    {
        _listBox.IsVisible = false;
        if (NoneTemplate is not null)
        {
            _stateLayer.ContentTemplate = NoneTemplate;
            _stateLayer.Content = null;
        }
        else
        {
            _stateLayer.ContentTemplate = null;
            _stateLayer.Content = null;
        }
    }
}
