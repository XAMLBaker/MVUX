using System.Runtime.CompilerServices;
using Mvux.Wpf.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Mvux.Wpf;

/// <summary>
/// 앱 전체 Selector의 Loaded/SelectionChanged를 전역 감지.
/// ItemsSource가 ISelectionFeed이면 SelectedItem 바인딩 없이 자동 동기화.
/// </summary>
internal static class SelectionSyncManager
{
    private static readonly ConditionalWeakTable<ISelectionFeed, Registry> _reg = new();

    static SelectionSyncManager()
    {
        EventManager.RegisterClassHandler(
            typeof(Selector), FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnLoaded));

        EventManager.RegisterClassHandler(
            typeof(Selector), Selector.SelectionChangedEvent,
            new SelectionChangedEventHandler(OnSelectionChanged));
    }

    public static void EnsureInitialized() { }

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
            if (!reg.IsSyncing)
                _ = sf.SetSelectedAsync(sel.SelectedItem);
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

            // 이미 알고 있는 선택 항목으로 초기화
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
