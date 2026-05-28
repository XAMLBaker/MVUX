using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Luke.Mvux;

namespace Luke.Mvux.Avalonia;

internal static class SelectionSyncManager
{
    private static readonly ConditionalWeakTable<ISelectionFeed, Registry> _reg = new();
    private static readonly ConditionalWeakTable<SelectingItemsControl, SelectionChangeTracker> _pendingChanges = new();
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

        // ItemsSource가 ISelectionFeed로 설정될 때 → Selector 등록
        ItemsControl.ItemsSourceProperty.Changed
            .AddClassHandler<SelectingItemsControl>((sel, e) => OnItemsSourceChanged(sel, e));

        // SelectionChanged → ISelectionFeed 업데이트
        SelectingItemsControl.SelectionChangedEvent
            .AddClassHandler<SelectingItemsControl>((sel, e) => OnSelectionChanged(sel, e));

        ObservableListFeedViewConfig.OnSelectionUpdated = UpdateSelection;
    }

    // ── ItemsSource 변경 ──────────────────────────────────────────────────────

    private static void OnItemsSourceChanged(SelectingItemsControl sel, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is ISelectionFeed { HasSelection: true } sf)
            GetRegistry(sf).Attach(sel);
    }

    // ── Selection 변경 → ISelectionFeed 업데이트 ──────────────────────────────

    private static void OnSelectionChanged(SelectingItemsControl sel, SelectionChangedEventArgs e)
    {
        if (sel.ItemsSource is ISelectionFeed { HasSelection: true } sf)
        {
            var reg = GetRegistry(sf);
            if (reg.IsSyncing)
                return;

            var tracker = _pendingChanges.GetOrCreateValue(sel);
            var version = ++tracker.Version;

            Dispatcher.UIThread.Post(() =>
            {
                if (tracker.Version != version || reg.IsSyncing)
                    return;

                _ = sf.SetSelectedAsync(SelectionInterop.ReadSelection(sel, sf));
            }, DispatcherPriority.Background);
        }
    }

    // ── IState 변경 → SelectingItemsControl 업데이트 ─────────────────────────

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
        private readonly List<WeakReference<SelectingItemsControl>> _selectors = new();
        private object? _lastSelection;

        public bool IsSyncing { get; private set; }

        public void Attach(SelectingItemsControl sel)
        {
            _selectors.RemoveAll(r => !r.TryGetTarget(out _));
            if (!_selectors.Any(r => r.TryGetTarget(out var s) && s == sel))
                _selectors.Add(new WeakReference<SelectingItemsControl>(sel));

            _pendingChanges.GetOrCreateValue(sel).Version++;
            IsSyncing = true;
            if (sel.ItemsSource is ISelectionFeed sf)
                SelectionInterop.ApplySelection(sel, sf, _lastSelection);
            IsSyncing = false;
        }

        public void UpdateAll(object? item)
        {
            _lastSelection = item;
            IsSyncing = true;
            foreach (var r in _selectors.ToList())
                if (r.TryGetTarget(out var sel))
                {
                    _pendingChanges.GetOrCreateValue(sel).Version++;
                    if (sel.ItemsSource is ISelectionFeed sf)
                        SelectionInterop.ApplySelection(sel, sf, item);
                }
            IsSyncing = false;
        }
    }

    private sealed class SelectionChangeTracker
    {
        public int Version;
    }
}
