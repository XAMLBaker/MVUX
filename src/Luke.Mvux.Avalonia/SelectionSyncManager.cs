using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Luke.Mvux;

namespace Luke.Mvux.Avalonia;

internal static class SelectionSyncManager
{
    private static readonly ConditionalWeakTable<ISelectionFeed, Registry> _reg = new();

    [ModuleInitializer]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255")]
    internal static void Initialize()
    {
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
            if (!reg.IsSyncing)
                _ = sf.SetSelectedAsync(sel.SelectedItem);
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

            IsSyncing = true;
            sel.SelectedItem = _lastSelection;
            IsSyncing = false;
        }

        public void UpdateAll(object? item)
        {
            _lastSelection = item;
            IsSyncing = true;
            foreach (var r in _selectors.ToList())
                if (r.TryGetTarget(out var sel))
                    sel.SelectedItem = item;
            IsSyncing = false;
        }
    }
}
