# MVUX — Claude 작업 지침

## 개발 시작 전 필독

새로운 개발 지시가 있을 때 **반드시 먼저 읽어야 할 파일**:

1. `.claude/docs/architecture.md` — 전체 아키텍처, 패턴, 설계 결정
2. `.claude/memory/MEMORY.md` — 누적된 피드백 및 프로젝트 컨텍스트

이 두 파일을 읽은 뒤 작업을 시작할 것.

---

## 프로젝트 개요

**MVUX** — Uno Platform MVUX의 API 설계(인터페이스명, 메서드 시그니처, 타입 구조)를 의도적으로 모방하여 WPF·Avalonia에서 독립적으로 재구현한 라이브러리. 소스 복사 없이 처음부터 새로 작성.

- 플랫폼: WPF, Avalonia (WinUI3, MAUI 제외)
- 실험적 기능: 별도 `extension` 프로젝트에서 진행
- 목적: 교육/데모용, Uno MVUX 패턴 전파
