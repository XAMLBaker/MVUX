using System.Runtime.CompilerServices;
using Luke.Mvux;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace Luke.Mvux.Wpf;

/// <summary>
/// 앱 전체 Selector의 Loaded/SelectionChanged를 전역 감지.
/// ItemsSource가 ISelectionFeed이면 SelectedItem 바인딩 없이 자동 동기화.
/// </summary>
internal static class SelectionSyncManager
{
    private static readonly ConditionalWeakTable<ISelectionFeed, Registry> _reg = new();
    private static readonly ConditionalWeakTable<Selector, SelectionChangeTracker> _trackers = new();
    private static readonly object _initGate = new();
    private static bool _initialized;

    [ModuleInitializer]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255")]
    internal static void Initialize()
    {
        lock (_initGate)
        {
            if (_initialized)
                return;

            _initialized = true;
        }

        EventManager.RegisterClassHandler(
            typeof(Selector), FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnLoaded));

        EventManager.RegisterClassHandler(
            typeof(Selector), Selector.SelectionChangedEvent,
            new SelectionChangedEventHandler(OnSelectionChanged));

        ObservableListFeedViewConfig.OnSelectionUpdated = UpdateSelection;
    }

    // ── Selector 이벤트 ───────────────────────────────────────────────────────

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Selector sel && GetFeed(sel) is { } sf)
            GetRegistry(sf).Attach(sel);
    }

    private static void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is Selector sel && GetFeed(sel) is { } sf)
        {
            var reg = GetRegistry(sf);
            var tracker = _trackers.GetOrCreateValue(sel);
            if (tracker.ApplyDepth > 0)
                return;

            var version = ++tracker.Version;

            sel.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
            {
                if (tracker.Version != version || tracker.ApplyDepth > 0)
                    return;

                _ = sf.SetSelectedAsync(SelectionInterop.ReadSelection(sel, sf));
            }));
        }
    }

    private static ISelectionFeed? GetFeed(Selector sel)
        => sel.ItemsSource is ISelectionFeed { HasSelection: true } sf ? sf : null;

    // ── IState 변경 → Selector 업데이트 ──────────────────────────────────────

    public static void UpdateSelection(ISelectionFeed sf, object? item)
    {
        if (_reg.TryGetValue(sf, out var reg))
            reg.UpdateAll(item);
    }

    private static Registry GetRegistry(ISelectionFeed sf)
        => _reg.GetOrCreateValue(sf);

    // ── Registry ──────────────────────────────────────────────────────────────

    internal sealed class Registry
    {
        private readonly List<WeakReference<Selector>> _selectors = [];
        private object? _lastSelection;

        public bool IsSyncing { get; private set; }

        public void Attach(Selector sel)
        {
            _selectors.RemoveAll(r => !r.TryGetTarget(out _));
            if (!_selectors.Any(r => r.TryGetTarget(out var s) && s == sel))
                _selectors.Add(new WeakReference<Selector>(sel));

            if (sel.ItemsSource is ISelectionFeed sf)
                ApplyToSelector(sel, sf, _lastSelection);
        }

        public void UpdateAll(object? item)
        {
            _lastSelection = item;
            foreach (var r in _selectors.ToList())
                if (r.TryGetTarget(out var sel))
                {
                    if (sel.ItemsSource is ISelectionFeed sf)
                        ApplyToSelector(sel, sf, item);
                }
        }

        private void ApplyToSelector(Selector sel, ISelectionFeed sf, object? item)
        {
            void ApplyCore()
            {
                var tracker = _trackers.GetOrCreateValue(sel);
                tracker.Version++;
                tracker.ApplyDepth++;
                IsSyncing = true;
                try
                {
                    SelectionInterop.ApplySelection(sel, sf, item);
                }
                finally
                {
                    tracker.ApplyDepth--;
                    IsSyncing = false;
                }
            }

            if (sel.Dispatcher.CheckAccess())
            {
                ApplyCore();
                return;
            }

            _ = sel.Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(ApplyCore));
        }
    }

    private sealed class SelectionChangeTracker
    {
        public int Version;
        public int ApplyDepth;
    }
}
