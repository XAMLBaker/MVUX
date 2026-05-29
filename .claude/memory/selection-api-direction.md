# Selection API Direction

- `Selection` 개념은 유지한다.
- `ListFeed.Async(...).WhenSelected(...)` 형태를 우선 지원한다.
- `WhenSelected(...)`는 리스트 feed에만 적용되도록 제한한다.
- 1차 구현은 단일 선택(`IState<T?>`)만 지원한다.
- 단일 선택 안정화 후 다중 선택(`IState<ImmutableList<T>>`) 오버로드를 추가한다.
- 목표는 MVVM 사용자에게 `Items`/`SelectedItem` 감각을 제공하면서도 MVUX selection 동기화 방식을 유지하는 것이다.
