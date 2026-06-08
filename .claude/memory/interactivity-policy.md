---
name: interactivity-policy
description: Keep cross-cutting UI interaction features inside platform packages before introducing a standalone package
metadata:
  type: project
---

`Extensions`는 기본 정책 용어로 사용하지 않는다. 이 저장소는 Uno 구현 확장이 아니라 독립 MVUX 구현체이며, `Extensions`는 `uno.extensions`를 연상시켜 경계를 흐릴 수 있다.

UI 상호작용 성격의 기능은 `Interactivity` 개념으로 분류한다. 예: behavior, attached property, 이벤트-커맨드 연결, 뷰 생명주기 연동.

현재 `Interactivity`는 별도 `Luke.Mvux.Interactivity` 패키지를 의미하지 않는다. 우선 `Luke.Mvux.Wpf` / `Luke.Mvux.Avalonia` 내부 폴더나 namespace로 관리한다.

별도 프로젝트/패키지 승격은 공통 API, 테스트, 문서, 릴리스 독립성이 충분히 커졌을 때만 검토한다.
