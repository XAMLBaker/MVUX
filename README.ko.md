# Luke.Mvux

[**English**](README.md)

Uno Platform [uno.extensions](https://github.com/unoplatform/uno.extensions)의 MVUX 패턴 API(`IFeed`, `IState`, `IListState`, `Option<T>`, `Message<T>` 등)를 그대로 모방하여 **WPF와 Avalonia**에서 독립적으로 재구현한 라이브러리입니다. `FeedView` 컨트롤, Roslyn 소스 제너레이터를 포함합니다.

> **이 프로젝트는** Uno Platform MVUX의 API 설계(인터페이스명, 메서드 시그니처, 타입 구조)를 의도적으로 동일하게 따라 만들었습니다. 소스 코드를 복사한 것이 아니라 WPF·Avalonia 환경에 맞게 처음부터 새로 구현했습니다. Uno Platform 팀과 공식적인 관계는 없습니다.

---

## 패키지

| 패키지 | 설명 | 대상 |
|--------|------|------|
| `Luke.Mvux.Wpf` | WPF용 FeedView 컨트롤 + 소스 제너레이터 | net8.0-windows / net10.0-windows |
| `Luke.Mvux.Avalonia` | Avalonia용 FeedView 컨트롤 + 소스 제너레이터 | net8.0 / net10.0 |

---

## 기능

- **`IFeed<T>` / `IState<T>` / `IListState<T>`** — 3단계 `Option<T>` (`Undefined` / `None` / `Some`)를 사용하는 반응형 비동기 데이터 스트림
- **독립 축을 가진 `Message<T>`** — `Data`, `IsLoading`, `Error`를 동시에 조합 가능 (예: 새로고침 중 기존 데이터 유지)
- **`FeedView` 컨트롤** — Loading / Data / Error / None 상태를 XAML에서 선언적으로 렌더링
- **Roslyn 소스 제너레이터** — `partial record *Model`만 작성하면 `*ViewModel` (INPC, 커맨드, 리스트 바인딩) 자동 생성
- **Selection 자동 동기화** — `listFeed.Selection(selectedState)` + `SelectedItem` 바인딩 없이 선택 동기화
- **`ListFeed` / `ListState`** — 완전한 CRUD 반응형 리스트 (`Add`, `AddRange`, `InsertAt`, `Remove`, `Update`, `Set`, `Clear`)

---

## 빠른 시작

### 1. 설치

**WPF**
```shell
dotnet add package Luke.Mvux.Wpf
```

**Avalonia**
```shell
dotnet add package Luke.Mvux.Avalonia
```

소스 제너레이터는 각 패키지에 내장되어 있어 별도 참조가 필요 없습니다.

### 2. Model 작성

```csharp
// WeatherModel.cs
public partial record WeatherModel(IWeatherService WeatherService)
{
    // IState<T>: 읽기/쓰기 반응형 상태
    public IState<string> City => State.Value(this, () => "Seoul");

    // IFeed<T>: City 변경 시 자동 재실행
    public IFeed<WeatherInfo> CurrentWeather =>
        City.SelectAsync((city, ct) => WeatherService.GetWeatherAsync(city, ct));

    // IListState<T>: 완전한 CRUD 반응형 리스트
    public IListState<string> Favorites => ListState.Value(this, () => new List<string>());
    public IState<string> SelectedFavorite => State<string>.Empty(this);

    // Selection: 항목 삭제 시 SelectedFavorite 자동 초기화
    public IListFeed<string> FavoritesWithSelection => Favorites.Selection(SelectedFavorite);

    // 커맨드: public ValueTask/Task, 파라미터 없거나 CancellationToken 하나
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

소스 제너레이터가 `WeatherViewModel`을 자동 생성합니다:
- `string? City { get; set; }` — `IState<string>`에 연결된 양방향 INPC 프로퍼티
- `IFeed<WeatherInfo> CurrentWeather` — `FeedView`를 위한 피드 패스스루
- `ObservableCollection<string> Favorites` / `FavoritesWithSelection` — 리스트 바인딩
- `ICommand AddFavorite` / `ICommand GoToSelected`

### 3. 코드 비하인드 연결

```csharp
public MainWindow()
{
    InitializeComponent();
    DataContext = new WeatherViewModel(new MyWeatherService());
}
```

### 4. XAML 바인딩

**WPF:**
```xml
<Window xmlns:lib="clr-namespace:Luke.Mvux.Wpf;assembly=Luke.Mvux.Wpf">

    <!-- IState<T> 양방향 바인딩 — 프로퍼티 이름 그대로 사용 -->
    <TextBox Text="{Binding City, UpdateSourceTrigger=PropertyChanged}" />

    <!-- FeedView: Loading / Data / Error 상태 처리 -->
    <lib:FeedView x:Name="WeatherFeed" Source="{Binding CurrentWeather}">

        <lib:FeedView.LoadingTemplate>
            <DataTemplate>
                <TextBlock Text="로딩 중..." />
            </DataTemplate>
        </lib:FeedView.LoadingTemplate>

        <lib:FeedView.FeedDataTemplate>
            <DataTemplate>
                <!-- DataContext는 FeedViewState -->
                <!-- {Binding Data}    — WeatherInfo 값      -->
                <!-- {Binding Refresh} — 재실행 ICommand      -->
                <!-- {Binding Parent}  — 상위 ViewModel       -->
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
                    <Button Content="재시도" Command="{Binding Refresh}" />
                </StackPanel>
            </DataTemplate>
        </lib:FeedView.ErrorTemplate>

    </lib:FeedView>

    <!-- RefreshCommand: 피드 구독 재시작 -->
    <Button Content="새로고침" Command="{Binding RefreshCommand, ElementName=WeatherFeed}" />

    <!-- ListView 선택 동기화 — SelectedItem 바인딩 불필요 -->
    <ListView ItemsSource="{Binding FavoritesWithSelection}" />

</Window>
```

**Avalonia** — XAML 구조는 동일하며, 네임스페이스와 `ListBox` 사용:
```xml
<Window xmlns:lib="clr-namespace:Luke.Mvux.Avalonia;assembly=Luke.Mvux.Avalonia">

    <!-- FeedView 사용법 동일 -->
    <lib:FeedView x:Name="WeatherFeed" Source="{Binding CurrentWeather}">
        ...
    </lib:FeedView>

    <!-- ListBox 선택 동기화 — SelectedItem 바인딩 불필요 -->
    <ListBox ItemsSource="{Binding FavoritesWithSelection}" />

</Window>
```

---

## API 개요

### Option\<T\>

| 상태 | 의미 |
|------|------|
| `Undefined` | 아직 모름 (초기 / 첫 로드 전) |
| `None` | 명시적으로 없음 (사용자가 지움) |
| `Some(T)` | 값 있음 |

### Message\<T\>

`Data`, `IsLoading`, `Error`는 **독립적인 축** — 동시에 조합 가능:

```csharp
Message<T>.Initial                          // Undefined + IsLoading=true
Message<T>.WithData(value)                  // Some(value)
Message<T>.WithData(value, isLoading: true) // 새로고침 중 기존 데이터 표시
Message<T>.Errored(ex)                      // 에러
Message<T>.None()                           // 명시적 없음
```

### 피드 연산자

```csharp
feed.Select(x => ...)                     // IFeed<TResult>
feed.SelectAsync((x, ct) => ...)          // IFeed<TResult>, 비동기 변환
feed.Where(x => ...)                      // IFeed<T>
feed.ForEachAsync(action, ct)             // 구독 루프

listFeed.Select(item => ...)              // IListFeed<TResult> (아이템 단위)
listFeed.Where(item => ...)               // IListFeed<T>       (아이템 단위)
listFeed.Selection(selectedState)         // 단일 선택 동기화
listFeed.Selection(selectedItemsState)    // 다중 선택 동기화
```

### IListState\<T\>

```csharp
await list.AddAsync(item);
await list.AddRangeAsync(items);
await list.InsertAtAsync(index, item);
await list.RemoveAsync(item);
await list.RemoveAsync(predicate);
await list.UpdateAsync(oldItem, newItem);
await list.UpdateAsync(predicate, updater);   // 첫 번째 일치 항목 업데이트
await list.UpdateAllAsync(predicate, updater); // 모든 일치 항목 업데이트
await list.SetAsync(items);
await list.ClearAsync();
```

---

## 프로젝트 구조

```
src/
  Luke.Mvux/                — 핵심 추상화 (플랫폼 무관, net8.0+)
  Luke.Mvux.Generators/     — Roslyn IIncrementalGenerator (netstandard2.0)
  Luke.Mvux.Wpf/            — WPF 컨트롤 + 제너레이터 번들 (net8.0-windows / net10.0-windows)
  Luke.Mvux.Avalonia/       — Avalonia 컨트롤 + 제너레이터 번들 (net8.0 / net10.0)
samples/
  Wpf.Sample/             — WPF 데모 앱 (날씨 + 즐겨찾기)
  Avalonia.Sample/        — Avalonia 데모 앱 (날씨 + 즐겨찾기)
tests/
  Mvux.Core.Tests/        — 단위 테스트 (xUnit)
```

---

## 라이선스

이 프로젝트는 **Apache License 2.0**으로 배포됩니다 — 자세한 내용은 [LICENSE](LICENSE) 파일을 참고하세요.

### 저작권 고지

이 프로젝트는 [Uno Platform MVUX (uno.extensions)](https://github.com/unoplatform/uno.extensions)의 API 설계를 의도적으로 모방하여 WPF·Avalonia에서 독립적으로 재구현했습니다. uno.extensions는 Apache License 2.0으로 배포됩니다.

> Copyright (c) 2021-present nventive and Uno Platform contributors
>
> Licensed under the Apache License, Version 2.0

uno.extensions의 소스 코드는 이 저장소에 포함되어 있지 않습니다. 인터페이스 설계와 패턴 구조를 따랐으며, WPF·Avalonia 구현 코드는 처음부터 새로 작성했습니다.
