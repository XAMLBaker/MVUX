using System.Collections;
using Luke.Mvux;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Luke.Mvux.Wpf;

public class FeedListView : ContentControl
{
    private readonly ListView _listView;
    private readonly ContentPresenter _stateLayer;
    private ISelectionFeed? _selFeed;
    private CancellationTokenSource _selCts = new();
    private bool _syncingSelection;

    // ── Dependency Properties ─────────────────────────────────────────────────

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(FeedListView),
            new PropertyMetadata(null, OnItemsSourceChanged));

    public static readonly DependencyProperty ItemTemplateProperty =
        DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(FeedListView),
            new PropertyMetadata(null, (d, e) => ((FeedListView)d)._listView.ItemTemplate = (DataTemplate?)e.NewValue));

    public static readonly DependencyProperty LoadingTemplateProperty =
        DependencyProperty.Register(nameof(LoadingTemplate), typeof(DataTemplate), typeof(FeedListView),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ErrorTemplateProperty =
        DependencyProperty.Register(nameof(ErrorTemplate), typeof(DataTemplate), typeof(FeedListView),
            new PropertyMetadata(null));

    public static readonly DependencyProperty NoneTemplateProperty =
        DependencyProperty.Register(nameof(NoneTemplate), typeof(DataTemplate), typeof(FeedListView),
            new PropertyMetadata(null));

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public DataTemplate? LoadingTemplate
    {
        get => (DataTemplate?)GetValue(LoadingTemplateProperty);
        set => SetValue(LoadingTemplateProperty, value);
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

    // ── Constructor ───────────────────────────────────────────────────────────

    public FeedListView()
    {
        _listView = new ListView { Visibility = Visibility.Collapsed };
        _listView.SelectionChanged += OnSelectionChanged;

        _stateLayer = new ContentPresenter();

        var root = new Grid();
        root.Children.Add(_stateLayer);
        root.Children.Add(_listView);
        Content = root;
    }

    // ── ItemsSource 변경 ──────────────────────────────────────────────────────

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((FeedListView)d).ApplyItemsSource(e.NewValue as IEnumerable);

    private void ApplyItemsSource(IEnumerable? source)
    {
        _selCts.Cancel();
        _selCts = new CancellationTokenSource();
        _selFeed = null;

        _listView.ItemsSource = source;

        if (source is ISelectionFeed { HasSelection: true } selFeed)
        {
            _selFeed = selFeed;
            _ = SubscribeSelectionAsync(selFeed, _selCts.Token);
        }

        // 컬렉션이 비어있으면 None 상태 표시
        if (source == null)
            ShowNone();
        else
            _listView.Visibility = Visibility.Visible;
    }

    private async Task SubscribeSelectionAsync(ISelectionFeed selFeed, CancellationToken ct)
    {
        try
        {
            await foreach (var msg in selFeed.GetSelectionMessages(ct))
            {
                if (ct.IsCancellationRequested) return;
                var captured = msg;
                Dispatcher.Invoke(() =>
                {
                    if (ct.IsCancellationRequested) return;
                    _syncingSelection = true;
                    _listView.SelectedItem = captured.HasData ? captured.DataObject : null;
                    _syncingSelection = false;
                });
            }
        }
        catch (OperationCanceledException) { }
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingSelection || _selFeed == null) return;
        _ = _selFeed.SetSelectedAsync(_listView.SelectedItem, _selCts.Token);
    }

    // ── State display ─────────────────────────────────────────────────────────

    private void ShowNone()
    {
        _listView.Visibility = Visibility.Collapsed;
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
