using System.Collections;
using System.Reflection;
using System.Windows.Controls.Primitives;

namespace Luke.Mvux.Wpf;

internal static class SelectionInterop
{
    public static object? ReadSelection(Selector selector, ISelectionFeed feed)
    {
        if (feed.SupportsMultipleSelection && TryGetSelectedItems(selector, out var selectedItems))
            return selectedItems.Cast<object?>().ToArray();

        return selector.SelectedItem;
    }

    public static void ApplySelection(Selector selector, ISelectionFeed feed, object? selection)
    {
        if (feed.SupportsMultipleSelection && TryGetSelectedItems(selector, out var selectedItems))
        {
            ReplaceSelectedItems(selectedItems, EnumerateSelection(selection));
            return;
        }

        selector.SelectedItem = EnumerateSelection(selection).FirstOrDefault();
    }

    private static bool TryGetSelectedItems(Selector selector, out IList selectedItems)
    {
        selectedItems = null!;

        var property = selector.GetType().GetProperty("SelectedItems", BindingFlags.Instance | BindingFlags.Public);
        if (property?.CanRead != true || !typeof(IList).IsAssignableFrom(property.PropertyType))
            return false;

        if (property.GetValue(selector) is not IList list)
            return false;

        selectedItems = list;
        return true;
    }

    private static void ReplaceSelectedItems(IList selectedItems, IEnumerable<object?> selection)
    {
        selectedItems.Clear();
        foreach (var item in selection)
            selectedItems.Add(item);
    }

    private static IEnumerable<object?> EnumerateSelection(object? selection)
    {
        if (selection is null)
            yield break;

        if (selection is IEnumerable items && selection is not string)
        {
            foreach (var item in items)
                yield return item;

            yield break;
        }

        yield return selection;
    }
}
