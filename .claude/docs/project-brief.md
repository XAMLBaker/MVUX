# MVUX Project Brief

이 문서는 세션 초반 재파악 비용을 줄이기 위한 짧은 요약이다. 구현 전 세부 규칙은 `architecture.md`와 `.claude/memory/MEMORY.md`를 함께 확인한다.

## 한 줄 요약

**Luke.Mvux**는 Uno Platform MVUX의 API 모양과 사용 패턴을 WPF/Avalonia에서 독립 구현한 교육/데모용 라이브러리다. Uno 소스 복사가 아니라 `IFeed`, `IState`, `IListState`, `Option<T>`, `Message<T>`, Source Generator, `FeedView` 경험을 재현하는 것이 목적이다.

## 현재 구조

| 경로 | 역할 |
|------|------|
| `src/Luke.Mvux` | 플랫폼 무관 코어. `IFeed`, `IState`, `IListState`, `Option<T>`, `Message<T>`, feed/state 구현 |
| `src/Luke.Mvux.Generators` | `partial record *Model`에서 `*ViewModel`을 생성하는 Roslyn generator |
| `src/Luke.Mvux.Wpf` | WPF `FeedView`, selection sync, generator 번들 패키지 |
| `src/Luke.Mvux.Avalonia` | Avalonia `FeedView`, selection sync, generator 번들 패키지 |
| `samples/*` | WPF/Avalonia 날씨 샘플 |
| `tests/Mvux.Core.Tests` | 코어 동작 xUnit 테스트 |

## 핵심 모델

- `Option<T>`는 `Undefined`, `None`, `Some(T)` 3단계 값이다.
- `Message<T>`는 `Data`, `IsLoading`, `Error`가 독립 축이다. 데이터가 있으면서 로딩 중인 stale refresh 상태가 가능하다.
- `IFeed<T>`는 읽기 전용 비동기 스트림이고, `IState<T>`는 쓰기 가능한 feed다.
- `IListFeed<T>`와 `IListState<T>`는 리스트 단위 메시지를 다루며, ViewModel에서는 `ObservableListFeedView<T>`를 통해 `ObservableCollection<T>`로 노출된다.

## 새 코드 작성 기준

- Model은 `public partial record SomethingModel(...)` 형태이며 이름은 `Model`로 끝낸다.
- 새 State/ListState 프로퍼티는 owner 패턴을 우선 사용한다: `State.Value(this, () => value)`, `State<T>.Empty(this)`, `ListState.Value(this, () => items)`.
- owner 없는 `State.Value(value)`, `State.Empty<T>()`, `ListState.Value(items)` 등은 기존 API/호환용으로 남아 있으나 새 예제의 기본값으로 삼지 않는다.
- `IFeed<T>` 프로퍼티는 보통 다른 state를 구독하는 연산자 조합으로 만든다: `City.SelectAsync((city, ct) => ...)`.
- 커맨드가 되어야 하는 Model 메서드는 `public`, `Task`/`ValueTask` 반환, 파라미터 없음 또는 `CancellationToken` 하나만 사용한다.

## Generator에서 특히 중요한 규칙

- `*Model`을 발견하면 `*ViewModel`을 생성한다.
- ViewModel 생성자는 Model 인스턴스를 받지 않는다. Model primary constructor 파라미터를 그대로 미러링하고 내부에서 `_model = new Model(...)`을 만든다.
- 생성된 ViewModel 클래스는 상속 가능해야 하므로 `sealed`로 만들지 않는다.
- `IState<T>`는 nullable property + INPC + `SetAsync`/`SetNoneAsync`로 노출한다.
- `IFeed<T>`는 그대로 pass-through하여 `FeedView`가 직접 구독하게 한다.
- `IListFeed<T>`/`IListState<T>`는 `ObservableCollection<T>`로 노출한다.

## FeedView와 Selection

- `FeedView`는 `IFeed`를 직접 구독해 `Loading`, `Data`, `Error`, `None` 템플릿을 선택한다.
- 템플릿 우선순위는 `HasData`, `Error`, `IsLoading`, `None` 순서다.
- 템플릿 DataContext는 `FeedViewState`이며 주요 바인딩은 `Data`, `Error`, `Progress`, `Refresh`, `Parent`다.
- Selection 동기화는 `listFeed.Selection(selectedState)`와 플랫폼별 `SelectionSyncManager`로 동작한다.
- WPF는 `Selector`, Avalonia는 `SelectingItemsControl` 기반이다. SelectedItem 바인딩 없이 ItemsSource만으로 동기화하는 것이 목표다.

## 범위 판단

- 현재 저장소와 아키텍처 문서 기준 지원 대상은 WPF와 Avalonia다.
- WinUI3, MAUI는 범위 밖이다.
- 실험적 기능이나 패턴에서 벗어난 시도는 코어에 바로 넣기보다 별도 extension 성격으로 분리한다.
- README는 사용자용 문서이고, 구현 판단은 `.claude/docs/architecture.md`와 `.claude/memory/*`를 우선한다.

## 확인 명령

일반 변경 후 기본 확인은 아래를 사용한다.

```shell
dotnet test "Mvux.slnx"
dotnet build "Mvux.slnx"
```

문서만 바꾼 경우에는 빌드/테스트를 생략할 수 있다.
