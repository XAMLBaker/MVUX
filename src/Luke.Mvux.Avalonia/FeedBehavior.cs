using System.Collections;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Luke.Mvux;

namespace Luke.Mvux.Avalonia;

public sealed class FeedBehavior : global::Avalonia.AvaloniaObject
{
    private static readonly ConditionalWeakTable<SelectingItemsControl, SelectorSubscription>
        _subscriptions = new();

    public static readonly AttachedProperty<IFeed?> ItemsSourceProperty =
        AvaloniaProperty.RegisterAttached<FeedBehavior, SelectingItemsControl, IFeed?>("ItemsSource");

    static FeedBehavior()
    {
        ItemsSourceProperty.Changed
            .AddClassHandler<SelectingItemsControl>((sel, e) => OnItemsSourceChanged(sel, e));
    }

    public static IFeed? GetItemsSource(SelectingItemsControl element)
        => element.GetValue(ItemsSourceProperty);

    public static void SetItemsSource(SelectingItemsControl element, IFeed? value)
        => element.SetValue(ItemsSourceProperty, value);

    private static void OnItemsSourceChanged(SelectingItemsControl selector, AvaloniaPropertyChangedEventArgs e)
    {
        if (_subscriptions.TryGetValue(selector, out var existing))
        {
            existing.Dispose();
            _subscriptions.Remove(selector);
        }

        if (e.NewValue is IFeed feed)
        {
            var sub = new SelectorSubscription(selector, feed);
            _subscriptions.Add(selector, sub);
        }
    }

    // ── Subscription ─────────────────────────────────────────────────────────

    private sealed class SelectorSubscription : IDisposable
    {
        private readonly SelectingItemsControl _selector;
        private readonly ISelectionFeed? _selFeed;
        private CancellationTokenSource _cts = new();
        private bool _syncingSelection;

        public SelectorSubscription(SelectingItemsControl selector, IFeed feed)
        {
            _selector = selector;
            _selFeed = feed as ISelectionFeed;

            selector.SelectionChanged += OnSelectionChanged;
            selector.Loaded += OnLoaded;
            selector.Unloaded += OnUnloaded;

            if (selector.IsLoaded)
                Start(feed);
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _cts.Cancel();
            _cts = new CancellationTokenSource();
            var feed = GetItemsSource(_selector);
            if (feed != null) Start(feed);
        }

        private void OnUnloaded(object? sender, RoutedEventArgs e)
            => _cts.Cancel();

        private void Start(IFeed feed)
        {
            _ = SubscribeListAsync(feed, _cts.Token);
            if (_selFeed != null)
                _ = SubscribeSelectionAsync(_selFeed, _cts.Token);
        }

        private async Task SubscribeListAsync(IFeed feed, CancellationToken ct)
        {
            try
            {
                await foreach (var msg in feed.GetMessages(ct))
                {
                    if (ct.IsCancellationRequested) return;
                    var captured = msg;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (ct.IsCancellationRequested) return;
                        _selector.ItemsSource = captured.HasData
                            ? (IEnumerable?)captured.DataObject
                            : null;
                    });
                }
            }
            catch (OperationCanceledException) { }
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
                        _selector.SelectedItem = captured.HasData ? captured.DataObject : null;
                        _syncingSelection = false;
                    });
                }
            }
            catch (OperationCanceledException) { }
        }

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_syncingSelection || _selFeed == null) return;
            _ = _selFeed.SetSelectedAsync(_selector.SelectedItem, _cts.Token);
        }

        public void Dispose()
        {
            _cts.Cancel();
            _selector.SelectionChanged -= OnSelectionChanged;
            _selector.Loaded -= OnLoaded;
            _selector.Unloaded -= OnUnloaded;
        }
    }
}
