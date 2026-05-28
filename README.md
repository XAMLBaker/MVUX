# Luke.Mvux

[**한국어**](README.ko.md)

A standalone reimplementation of the [Uno Platform MVUX](https://github.com/unoplatform/uno.extensions) pattern API (`IFeed`, `IState`, `IListState`, `Option<T>`, `Message<T>`, etc.) for **WPF and Avalonia**. Includes `FeedView` control and Roslyn source generator.

> **This project** intentionally mirrors the API design (interface names, method signatures, type structures) of Uno Platform MVUX. The implementation was written from scratch for WPF and Avalonia — no source code was copied. This project has no official affiliation with the Uno Platform team.

---

## Packages

| Package | Description | Target |
|---------|-------------|--------|
| `Luke.Mvux.Wpf` | FeedView control + source generator for WPF | net8.0-windows / net10.0-windows |
| `Luke.Mvux.Avalonia` | FeedView control + source generator for Avalonia | net8.0 / net10.0 |

---

## Features

- **`IFeed<T>` / `IState<T>` / `IListState<T>`** — reactive async data streams with a 3-state `Option<T>` (`Undefined` / `None` / `Some`)
- **`Message<T>` with independent axes** — `Data`, `IsLoading`, and `Error` can be combined simultaneously (e.g. stale data while refreshing)
- **`FeedView` control** — renders Loading / Data / Error / None states declaratively in XAML
- **Roslyn Source Generator** — write a `partial record *Model`, get a full `*ViewModel` (INPC, commands, list bindings) automatically
- **Selection sync** — `listFeed.Selection(selectedState)` + `ListView` with zero `SelectedItem` binding
- **`ListFeed` / `ListState`** — full CRUD reactive list (`Add`, `AddRange`, `InsertAt`, `Remove`, `Update`, `Set`, `Clear`)

---

## Quick Start

### 1. Install

**WPF**
```shell
dotnet add package Luke.Mvux.Wpf
```

**Avalonia**
```shell
dotnet add package Luke.Mvux.Avalonia
```

The source generator is bundled inside each package — no separate generator reference needed.

### 1.5 Initialize MVUX once at app startup

Selection sync is registered at platform startup. Add `UseMvux()` once:

**WPF (`App.xaml.cs`)**
```csharp
using Luke.Mvux.Wpf;

protected override void OnStartup(StartupEventArgs e)
{
    this.UseMvux();
    base.OnStartup(e);
}
```

**Avalonia (`Program.cs`)**
```csharp
using Luke.Mvux.Avalonia;

public static AppBuilder BuildAvaloniaApp() =>
    AppBuilder.Configure<App>()
        .UseMvux()
        .UsePlatformDetect()
        .LogToTrace();
```

### 2. Write a Model

```csharp
// WeatherModel.cs
public partial record WeatherModel(IWeatherService WeatherService)
{
    // IState<T>: readable/writable reactive state
    public IState<string> City => State.Value(this, () => "Seoul");

    // IFeed<T>: re-executes automatically whenever City changes
    public IFeed<WeatherInfo> CurrentWeather =>
        City.SelectAsync((city, ct) => WeatherService.GetWeatherAsync(city, ct));

    // IListState<T>: full-CRUD reactive list
    public IListState<string> Favorites => ListState.Value(this, () => new List<string>());
    public IState<string> SelectedFavorite => State<string>.Empty(this);

    // Selection: auto-clears SelectedFavorite when the item is removed
    public IListFeed<string> FavoritesWithSelection => Favorites.Selection(SelectedFavorite);

    // Commands: public ValueTask/Task, 0 params or CancellationToken only
    public async ValueTask AddFavorite(CancellationToken ct)
    {
        var city = await City;
        if (city != null) await Favorites.AddAsync(city, ct);
    }

    public async ValueTask GoToSelected(CancellationToken ct)
    {
        var selected = await SelectedFavorite;
        if (selected != null) await City.SetAsync(selected, ct);
    }
}
```

The source generator produces `WeatherViewModel` automatically with:
- `string? City { get; set; }` — two-way INPC property wired to `IState<string>`
- `IFeed<WeatherInfo> CurrentWeather` — raw feed passthrough for `FeedView`
- `ObservableCollection<string> Favorites` / `FavoritesWithSelection` — list bindings
- `ICommand AddFavorite` / `ICommand GoToSelected`

### 3. Wire up in code-behind

```csharp
public MainWindow()
{
    InitializeComponent();
    DataContext = new WeatherViewModel(new MyWeatherService());
}
```

### 4. Bind in XAML

**WPF:**
```xml
<Window xmlns:lib="clr-namespace:Luke.Mvux.Wpf;assembly=Luke.Mvux.Wpf">

    <!-- IState<T> two-way binding — just use the property name -->
    <TextBox Text="{Binding City, UpdateSourceTrigger=PropertyChanged}" />

    <!-- FeedView: handles Loading / Data / Error states -->
    <lib:FeedView x:Name="WeatherFeed" Source="{Binding CurrentWeather}">

        <lib:FeedView.LoadingTemplate>
            <DataTemplate>
                <TextBlock Text="Loading..." />
            </DataTemplate>
        </lib:FeedView.LoadingTemplate>

        <lib:FeedView.FeedDataTemplate>
            <DataTemplate>
                <!-- DataContext here is FeedViewState -->
                <!-- {Binding Data}    — the WeatherInfo value    -->
                <!-- {Binding Refresh} — ICommand to re-fetch     -->
                <!-- {Binding Parent}  — the parent ViewModel     -->
                <StackPanel>
                    <TextBlock Text="{Binding Data.City}" FontSize="24" FontWeight="Bold" />
                    <TextBlock Text="{Binding Data.Temperature}" FontSize="48" />
                </StackPanel>
            </DataTemplate>
        </lib:FeedView.FeedDataTemplate>

        <lib:FeedView.ErrorTemplate>
            <DataTemplate>
                <StackPanel>
                    <TextBlock Foreground="Red" Text="{Binding Error.Message}" />
                    <Button Content="Retry" Command="{Binding Refresh}" />
                </StackPanel>
            </DataTemplate>
        </lib:FeedView.ErrorTemplate>

    </lib:FeedView>

    <!-- RefreshCommand restarts the feed subscription -->
    <Button Content="Refresh" Command="{Binding RefreshCommand, ElementName=WeatherFeed}" />

    <!-- ListView selection sync — no SelectedItem binding needed -->
    <ListView ItemsSource="{Binding FavoritesWithSelection}" />

</Window>
```

**Avalonia** — same XAML structure, different namespace and `ListBox` instead of `ListView`:
```xml
<Window xmlns:lib="clr-namespace:Luke.Mvux.Avalonia;assembly=Luke.Mvux.Avalonia">

    <!-- same FeedView usage -->
    <lib:FeedView x:Name="WeatherFeed" Source="{Binding CurrentWeather}">
        ...
    </lib:FeedView>

    <!-- ListBox selection sync — no SelectedItem binding needed -->
    <ListBox ItemsSource="{Binding FavoritesWithSelection}" />

</Window>
```

---

## API Overview

### Option\<T\>

| State | Meaning |
|-------|---------|
| `Undefined` | Not yet known (initial / before first load) |
| `None` | Explicitly empty (user cleared it) |
| `Some(T)` | Has a value |

### Message\<T\>

`Data`, `IsLoading`, and `Error` are **independent axes** — they can be combined:

```csharp
Message<T>.Initial                       // Undefined + IsLoading=true
Message<T>.WithData(value)               // Some(value)
Message<T>.WithData(value, isLoading: true) // stale data while refreshing
Message<T>.Errored(ex)                   // Error
Message<T>.None()                        // explicitly empty
```

### Feed operators

```csharp
feed.Select(x => ...)                     // IFeed<TResult>
feed.SelectAsync((x, ct) => ...)          // IFeed<TResult>, async transform
feed.Where(x => ...)                      // IFeed<T>
feed.ForEachAsync(action, ct)             // subscribe loop

listFeed.Select(item => ...)              // IListFeed<TResult> (item-level)
listFeed.Where(item => ...)               // IListFeed<T>       (item-level)
listFeed.Selection(selectedState)         // single selection sync
listFeed.Selection(selectedItemsState)    // multi-selection sync
```

### IListState\<T\>

```csharp
await list.AddAsync(item);
await list.AddRangeAsync(items);
await list.InsertAtAsync(index, item);
await list.RemoveAsync(item);
await list.RemoveAsync(predicate);
await list.UpdateAsync(oldItem, newItem);
await list.UpdateAsync(predicate, updater);   // extension: first match
await list.UpdateAllAsync(predicate, updater); // extension: all matches
await list.SetAsync(items);
await list.ClearAsync();
```

---

## Project Structure

```
src/
  Luke.Mvux/                — Core abstractions (platform-agnostic, net8.0+)
  Luke.Mvux.Generators/     — Roslyn IIncrementalGenerator (netstandard2.0)
  Luke.Mvux.Wpf/            — WPF controls + Generator bundle (net8.0-windows / net10.0-windows)
  Luke.Mvux.Avalonia/       — Avalonia controls + Generator bundle (net8.0 / net10.0)
samples/
  Wpf.Sample/             — WPF demo app (weather + favorites)
  Avalonia.Sample/        — Avalonia demo app (weather + favorites)
tests/
  Mvux.Core.Tests/        — Unit tests (xUnit)
```

---

## License

This project is licensed under the **Apache License 2.0** — see the [LICENSE](LICENSE) file for details.

### Attribution

This project reimplements the API design of [Uno Platform MVUX (uno.extensions)](https://github.com/unoplatform/uno.extensions) independently for WPF and Avalonia. uno.extensions is distributed under the Apache License 2.0.

> Copyright (c) 2021-present nventive and Uno Platform contributors
>
> Licensed under the Apache License, Version 2.0

The source code of uno.extensions is not included in this repository. The interface design and pattern structure were followed; the WPF and Avalonia implementation code was written from scratch.
