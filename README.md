# MVUX.Wpf

Uno Platform [uno.extensions](https://github.com/unoplatform/uno.extensions)의 MVUX 패턴 API(`IFeed`, `IState`, `IListState`, `Option<T>`, `Message<T>` 등)를 그대로 모방하여 WPF에서 독립적으로 재구현한 라이브러리입니다. `FeedView` 컨트롤, Roslyn 소스 제너레이터를 포함합니다.

> **이 프로젝트는** Uno Platform MVUX의 API 설계(인터페이스명, 메서드 시그니처, 타입 구조)를 의도적으로 동일하게 따라 만들었습니다. 소스 코드를 복사한 것이 아니라 WPF 환경에 맞게 처음부터 새로 구현했습니다. Uno Platform 팀과 공식적인 관계는 없습니다.

---

## Features

- **`IFeed<T>` / `IState<T>` / `IListState<T>`** — reactive async data streams with a 3-state `Option<T>` (`Undefined` / `None` / `Some`)
- **`Message<T>` with independent axes** — `Data`, `IsLoading`, and `Error` can be combined simultaneously (e.g. stale data while refreshing)
- **`FeedView` control** — renders Loading / Data / Error / None states declaratively in XAML
- **Roslyn Source Generator** — write a `partial record *Model`, get a full `*ViewModel` (INPC, commands, list bindings) automatically
- **Selection sync** — `listFeed.Selection(selectedState)` + `ListView` with zero `SelectedItem` binding
- **`ListFeed` / `ListState`** — full CRUD reactive list (`Add`, `AddRange`, `InsertAt`, `Remove`, `Update`, `Set`, `Clear`)

---

## Requirements

| Component | Target |
|-----------|--------|
| `Mvux.Wpf.Core` | .NET 8.0+ |
| `Mvux.Wpf` + `FeedView` | .NET 10.0-windows (WPF) |
| Source Generator | Roslyn (bundled in `Mvux.Wpf`) |

---

## Quick Start

### 1. Reference

```xml
<!-- Wpf.Sample.csproj -->
<ItemGroup>
  <ProjectReference Include="src/Mvux.Wpf.Core/Mvux.Wpf.Core.csproj" />
  <ProjectReference Include="src/Mvux.Wpf/Mvux.Wpf.csproj" />
  <!-- Generator is bundled in Mvux.Wpf as an analyzer -->
</ItemGroup>
```

### 2. Write a Model

```csharp
// WeatherModel.cs
public partial record WeatherModel(IWeatherService WeatherService)
{
    // IState<T>: readable/writable reactive state
    public IState<string> City { get; } = State.Value("Seoul");

    // IFeed<T>: re-executes automatically whenever City changes
    public IFeed<WeatherInfo> CurrentWeather =>
        City.SelectAsync((city, ct) => WeatherService.GetWeatherAsync(city, ct));

    // IListState<T>: full-CRUD reactive list
    public IListState<string> Favorites { get; } = ListState.Empty<string>();
    public IState<string> SelectedFavorite { get; } = State.Empty<string>();

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
- `ICommand AddFavoriteCommand` / `ICommand GoToSelectedCommand`

### 3. Wire up in code-behind

```csharp
public MainWindow()
{
    InitializeComponent();
    DataContext = new WeatherViewModel(new MyWeatherService());
}
```

### 4. Bind in XAML

```xml
<Window xmlns:lib="clr-namespace:Mvux.Wpf;assembly=Mvux.Wpf">

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
  Mvux.Wpf.Core/        — Core abstractions (platform-agnostic, net8.0+)
  Mvux.Wpf/             — WPF controls + Generator bundle (net10.0-windows)
  Mvux.Wpf.Generators/  — Roslyn IIncrementalGenerator (netstandard2.0)
samples/
  Wpf.Sample/           — Demo app (weather + favorites)
tests/
  Mvux.Wpf.Core.Tests/  — Unit tests (xUnit, 42 tests)
```

---

## License

This project is licensed under the **Apache License 2.0** — see the [LICENSE](LICENSE) file for details.

### Attribution

이 프로젝트는 [Uno Platform MVUX (uno.extensions)](https://github.com/unoplatform/uno.extensions)의 API 설계를 의도적으로 모방하여 WPF에서 독립적으로 재구현했습니다. uno.extensions는 Apache License 2.0으로 배포됩니다.

> Copyright (c) 2021-present nventive and Uno Platform contributors
>
> Licensed under the Apache License, Version 2.0

uno.extensions의 소스 코드는 이 저장소에 포함되어 있지 않습니다. 인터페이스 설계와 패턴 구조를 따랐으며, WPF 구현 코드는 처음부터 새로 작성했습니다.
