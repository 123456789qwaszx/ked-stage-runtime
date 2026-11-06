# Ked.Presentation — 연출 런타임

비주얼 노벨의 연출을 **계산하고**(Core), **무대에서 실행하고**(Stage), **대사 한 줄씩 재생한다**(Playback).

## 어셈블리 — 폴더 하나에 어셈블리 하나

| 폴더 | 어셈블리 | 하는 일 | 참조 |
|---|---|---|---|
| `Core/` | `Ked.Presentation.Core` | 계산. 커맨드가 끝나면 노드가 어떤 값이어야 하는가(`NodeTargets` · `ShotIntents`), 커맨드를 정지 프레임으로 접는 리듀서, 대본 낱말(`12fr` · `2u`), 이징·모션 수학, 튜닝 형식. 순수 C#(엔진 참조 0)이라 저작 툴과 같은 코드를 쓴다 | 없음 |
| `Stage/` | `Ked.Presentation.Stage` | 무대 실행. 캐릭터·무대 리그, 대본 문법의 뜻(`StageCommandParser`), 커맨드와 트윈, 샷, 화면 효과, 오디오. 리그 프리팹 · 셰이더 · 프리셋 기본값도 여기 있다 | Core · Ked.CommandExecutor · DOTween |
| `Playback/` | `Ked.Presentation.Playback` | 대사 재생. Yarn 연결, 대사창·줄 표시, 진행 입력·자동·스킵, 되감기·백로그(연출 기억), 등가성 하네스 | Core · Stage · Ked.CommandExecutor · Yarn Spinner · TMP · UGUI |
| `Editor/` | `Ked.Presentation.Editor` | 에디터 도구. 튜닝·모션 내보내기, 초상 DB 빌더 | Core · Stage |
| `Tests/EditMode/` | `Ked.Presentation.Tests.EditMode` | EditMode 테스트(Unity 계산과의 대조 포함) | Core · Stage · Editor |

## Stage 안 — 기능 하나에 폴더 하나

시스템과 그것을 움직이는 커맨드가 같은 폴더에 산다. 예외는 대본 문법의 뜻 하나(`Grammar/`)다.

| 폴더 | 무엇 |
|---|---|
| `PresentationStage.cs` | 무대 하나의 상태 — 모든 기능의 입구 |
| `IntentTransition.cs` | 의도 하나를 따라가는 전환 — 샷 · 화면 효과 · 블러 · 캐릭터 스타일이 함께 쓴다 |
| `Grammar/` | 대본 → 스펙. `StageCommandParser`(표 + 기능별 partial), `Tokens/` 낱말 해석 |
| `StageRig/` | 무대 리그 — Stage00/01/02 · 깊이 층 · 마운트 |
| `Character/` | 캐릭터 리그 — `Rig/` 스키마·빌더·레지스트리 · `Portrait/` 초상 · `Placement/` 정착 장부·초점·배치·깊이 계산 · `Visual/` 비주얼 · `Motion/` 모션 클립 · `Commands/` |
| `Shot/` | 카메라 Pan·Zoom과 카메라 중립 계약, 샷 커맨드 |
| `ScreenEffect/` | flash · noise · vignette · 블러 의도 · 전환 마스크(`StageMask/`), 그 커맨드 |
| `Audio/` | BGM · SFX · 보이스, 그 커맨드 |
| `Control/` | wait · actor 별칭 · 앞뒤 순서 |
| `Equivalence/` · `CoreBridge/` · `Diagnostics/` | 실제 무대 캡처 · 유니티↔코어 변환 · 초점 디버그 뷰 |
| `Prefabs/` · `Resources/` · `Presets/` | 리그 프리팹 · 셰이더와 재질 · 커맨드 프리셋과 튜닝 기본값 |

## 의존 방향

```
예제(Assembly-CSharp) ──> Playback ──> Stage ──> Core
                                        └──> Ked.CommandExecutor
Editor ──> Stage
```

언제나 아래로만 향한다. Core는 아무것도 모르고, Stage는 Yarn을 모른다.

## 원본과 사본

이 저장소가 **원본**이다.
- `Core/`는 저작 툴(ked-story-compositor)에 `src/Ked.Presentation.Core`로 복사 반입돼 있다.
- Playback을 뺀 나머지는 포트폴리오용 분리본 ked-stage-runtime에 한 벌 있다.

## 더 읽을 곳

`Documentation~/`는 Unity Project 창에 보이지 않는다(파일 탐색기나 GitHub에서 연다).
- `Documentation~/ASSEMBLY-BOUNDARY.md` — 경계를 이렇게 그은 이유
- `Documentation~/vendoring.md` — 분리본 맞추기
- `Documentation~/BLUR-DECISIONS.md` — 흐림 효과의 결정 기록
- `Core/Documentation~/target-boundary.md` — 목표 계산을 순수 함수로 떼어 낸 규약과 이유(값 하나가 장부 · 트윈 · 정지 프레임에 쓰인다)
- `Core/Documentation~/transform-math-and-epsilon.md` — 코어 좌표 계산의 규약과 허용 오차
