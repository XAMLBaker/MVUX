# Multi-selection samples

## Startup requirement

- WPF: call `this.UseMvux()` in `App.OnStartup`
- Avalonia: call `.UseMvux()` in `BuildAvaloniaApp()`

## Quick verification checklist

- Click one item in the left list, confirm the right panel updates.
- Use Ctrl/Shift multi-select, confirm all selected items appear on the right.
- Click `Select Recommended`, then click a single different item and confirm selection sync stays stable.
- Click `Clear Selection` and confirm both left and right selections are cleared.
- Remove selected items and confirm stale selections are pruned.
