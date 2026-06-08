---
name: project-intent
description: Project goals — MVUX evangelism, WPF/Avalonia focus, and clear boundaries between core and UI interaction features
metadata:
  type: project
---

프로젝트 이름: **Luke.Mvux / MVUX**

Uno MVUX 패턴을 WPF와 Avalonia에서 재현해 전파하는 것이 목적. 교육/데모 용도.

**Why:** Uno Platform의 MVUX 패턴이 좋지만 인지도가 낮음. WPF/Avalonia 개발자에게 동일한 패턴을 경험시키려는 목적.

**How to apply:**
- 플랫폼은 WPF와 Avalonia에 집중 (WinUI3, MAUI는 보류)
- 코어 패턴은 플랫폼 중립 모델에 집중하고, UI 상호작용 기능은 우선 각 플랫폼 패키지 내부에서 관리
- 코어 패턴은 Uno MVUX와 최대한 동일하게 유지
