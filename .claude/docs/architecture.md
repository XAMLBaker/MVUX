# MVUX.Wpf 아키텍처 문서

## 프로젝트 구조

```
src/
  Mvux.Wpf.Core/          - 핵심 추상화 (플랫폼 무관)
  Mvux.Wpf/               - WPF 컨트롤 (FeedView, ObservableListFeedView 등)
  Mvux.Wpf.Generators/    - Roslyn Source Generator
samples/
  Wpf.Sample/             - WPF 데모 앱
tests/
  Mvux.Wpf.Core.Tests/    - 단위 테스트 (42개)
```

---

## 핵심 타입 (Mvux.Wpf.Core)

### 인터페이스

| 타입 | 설명 |
|------|------|
| `IFeed` | non-generic 베이스. `GetMessages(ct)` → `IAsyncEnumerable<IMessage>` |
| `IFeed<T>` | 읽기 전용 비동기 데이터 스트림. `GetSource(ct)` → `IAsyncEnumerable<Message<T>>` |
| `IState<T>` | 읽기/쓰기 반응형 상태. `SetAsync`, `UpdateAsync(Func<Option<T>,Option<T>>)`, `UpdateAsync(Func<T?,T?>)` |
| `IListFeed<T>` | `IFeed<IReadOnlyList<T>>` 확장 |
| `IListState<T>` | `IListFeed<T>` 확장. Add/AddRange/InsertAt/Remove/Update/Set/Clear 제공 |
| `ISelectionFeed` | `HasSelection`, `SetSelectedAsync`, `GetSelectionMessages` — 선택 자동 동기화용 |

### Option\<T\> — 3단계 값

Uno의 `Option<T>`와 동일한 3단계 구조:

| 상태 | 의미 |
|------|------|
| `Undefined` | 아직 모름 (초기/로딩 전) |
| `None` | 명시적으로 없음 (사용자가 지웠을 때) |
| `Some(T)` | 값 있음 |

### Message\<T\> — 독립 축

`Data`, `IsLoading`, `Error`가 **독립적**으로 조합 가능 (Uno의 axes 방식 모방).

```csharp
Message<T>.Initial          // Undefined + IsLoading=true  (초기 상태)
Message<T>.Loading()        // Undefined + IsLoading=true
Message<T>.WithData(value)  // Some(value) + IsLoading=false
Message<T>.WithData(value, isLoading: true)  // stale 데이터 보여주면서 새로고침 중
Message<T>.Errored(ex)      // None + Error=ex
Message<T>.None()           // None (명시적 없음)
```

`Data` 프로퍼티 타입은 `Option<T>`. `HasData`, `IsNone`, `IsUndefined` computed 프로퍼티 제공.

### 팩토리 클래스

| 클래스 | 메서드 | 설명 |
|--------|--------|------|
| `Feed` | `Async(fetch)` | 단발 비동기 피드 |
| `State` | `Value(v)`, `Empty<T>()`, `Async(fetch)` | 상태 생성 |
| `ListFeed` | `Async(fetch)`, `AsyncEnumerable(source)` | 읽기 전용 리스트 피드 |
| `ListState` | `Empty<T>()`, `Value(items)` | 변경 가능한 리스트 상태 |

---

## MVUX 패턴

### Model 작성 규칙

```csharp
// partial record, 이름은 반드시 "Model"로 끝나야 함
public partial record WeatherModel(IWeatherService WeatherService)
{
    public IState<string> City { get; } = State.Value("Seoul");

    // IFeed<T>: City 변경 시 자동 재실행 (SelectAsyncFeed가 City를 구독)
    public IFeed<WeatherInfo> CurrentWeather =>
        City.SelectAsync((city, ct) => WeatherService.GetWeatherAsync(city, ct));

    public IListState<string> Favorites { get; } = ListState.Empty<string>();
    public IState<string> SelectedFavorite { get; } = State.Empty<string>();

    // Selection: Favorites에 SelectedFavorite 연결, 항목 사라지면 자동 초기화
    public IListFeed<string> FavoritesWithSelection => Favorites.Selection(SelectedFavorite);

    // 커맨드: public, ValueTask/Task 반환, 파라미터 없거나 CancellationToken 하나
    public async ValueTask AddFavorite(CancellationToken ct) { ... }
    public async ValueTask GoToSelected(CancellationToken ct) { ... }
}
```

### ViewModel 자동 생성 (ViewModelGenerator)

Generator가 `*Model` partial record를 감지해 `*ViewModel`을 자동 생성.

| Model 타입 | ViewModel 노출 방식 |
|-----------|-------------------|
| `IFeed<T>` | `public IFeed<T> Name => _model.Name;` — FeedView가 직접 구독 |
| `IState<T>` | `public T? Name { get; set; }` + INPC + `BeginInvoke` + SetAsync/SetNoneAsync |
| `IListFeed<T>` / `IListState<T>` | `public ObservableCollection<T> Name` — `ObservableListFeedView<T>` 래핑 |
| `ValueTask/Task` 메서드 (0~1 CT 파라미터) | `public ICommand Name { get; }` — AsyncCommand 래핑 |

### ViewModel 생성자 패턴

```csharp
// Model primary constructor 파라미터를 미러링
DataContext = new WeatherViewModel(new FakeWeatherService());
```

> Model 인스턴스를 직접 주입하는 패턴(`WeatherViewModel(WeatherModel)`)은 사용하지 않음.

---

## FeedView (Mvux.Wpf)

`IFeed`를 직접 구독해 Loading / Data / Error / None 상태를 렌더링하는 WPF `ContentControl`.

```xml
<lib:FeedView x:Name="WeatherFeed" Source="{Binding CurrentWeather}">
    <lib:FeedView.LoadingTemplate><DataTemplate>...</DataTemplate></lib:FeedView.LoadingTemplate>
    <lib:FeedView.FeedDataTemplate><DataTemplate>...</DataTemplate></lib:FeedView.FeedDataTemplate>
    <lib:FeedView.ErrorTemplate><DataTemplate>...</DataTemplate></lib:FeedView.ErrorTemplate>
</lib:FeedView>

<!-- RefreshCommand: 구독 재시작 → City 현재값 replay → 재실행 -->
<Button Command="{Binding RefreshCommand, ElementName=WeatherFeed}" />
```

### 템플릿 DataContext = FeedViewState

| 바인딩 | 타입 | 설명 |
|--------|------|------|
| `{Binding Data}` | `object?` | 현재 데이터 |
| `{Binding Error}` | `Exception?` | 에러 |
| `{Binding Progress}` | `bool` | 로딩 중 여부 (Data와 동시 가능) |
| `{Binding Refresh}` | `ICommand` | 재실행 커맨드 |
| `{Binding Parent}` | `object?` | 상위 DataContext (ViewModel) |

### 우선순위 규칙

1. `HasData` → FeedDataTemplate (Progress=true면 새로고침 중)
2. `Error != null` → ErrorTemplate
3. `IsLoading` → LoadingTemplate
4. else → NoneTemplate

---

## Selection 자동 동기화

`SelectionSyncManager`가 `EventManager.RegisterClassHandler`로 앱 전역 모든 `Selector` 이벤트를 후킹. **SelectedItem 바인딩 없이** ItemsSource 하나로 선택 자동 동기화.

```xml
<!-- SelectedItem 바인딩 불필요 -->
<ListView ItemsSource="{Binding FavoritesWithSelection}" />
```

동작 원리:
1. `ObservableListFeedView<T>`가 `ISelectionFeed` 구현 (SelectionListFeed 위임)
2. `SelectionSyncManager`가 Selector.Loaded → `ObservableListFeedView` 등록
3. `SelectionChanged` → `ISelectionFeed.SetSelectedAsync` 호출
4. State 변경 → `SelectionSyncManager.UpdateAll` → 모든 등록된 Selector.SelectedItem 업데이트

---

## FeedExtensions — 주요 연산자

```csharp
// IFeed 연산자
feed.Select(x => ...)            // IFeed<TResult>
feed.SelectAsync((x, ct) => ...) // IFeed<TResult>, 비동기 변환
feed.Where(x => ...)             // IFeed<T>, 메시지 단위 필터
feed.ForEachAsync(action, ct)    // 구독 루프

// IListFeed 연산자 (아이템 단위, IListFeed 타입 보존)
listFeed.Select(item => ...)     // IListFeed<TResult>
listFeed.Where(item => ...)      // IListFeed<T>

// Selection
listFeed.Selection(selectedState)         // 단일 선택
listFeed.Selection(selectedItemsState)    // 다중 선택 (ImmutableList)

// IState 헬퍼
state.SetNoneAsync(ct)
state.UpdateAsync(async (v, ct) => ...)

// IListState 헬퍼
listState.UpdateAsync(predicate, updater)    // 조건 맞는 첫 항목 업데이트
listState.UpdateAllAsync(predicate, updater) // 조건 맞는 모든 항목 업데이트
```

---

## 스레딩 모델

| 컴포넌트 | 방식 |
|---------|------|
| FeedView | `Dispatcher.Invoke` — 피드 메시지를 UI 스레드로 마샬링 |
| IState 구독 (Generator) | 생성 시점 `Dispatcher.CurrentDispatcher` 캡처 + `BeginInvoke` |
| ObservableListFeedView | `Dispatcher.Invoke` — 리스트 업데이트 마샬링 |

---

## XAML 네임스페이스

```xml
xmlns:lib="clr-namespace:Mvux.Wpf;assembly=Mvux.Wpf"
```
