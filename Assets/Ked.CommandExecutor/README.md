# Ked.CommandExecutor — 커맨드 실행기

커맨드의 **수명**을 돌린다 — 스펙에서 커맨드를 만들고, 순서대로 재생하고, 스킵·스텝 경계·취소를 처리한다.
무대도 비주얼 노벨도 모른다. 무엇을 움직이는지는 커맨드를 만드는 쪽(`Ked.Presentation.Stage`)이 정한다.

## 어셈블리 — 하나뿐이라 이 폴더가 곧 어셈블리다

| 자리 | 하는 일 |
|---|---|
| `Base/` | 커맨드 기반 타입 — `CommandSpecBase` · `CommandBase` · 트윈 하나로 노드를 잡는 `ClaimTweenCommandBase` · 확정 때 함께 풀 예약(`CommitReleases`) · 팩토리 계약 |
| `Execution/` | 실행 — `CommandExecutor` · `SequencePlayer` · 스킵 정책 · 호스트가 채우는 포트 |
| 뿌리 | 실행 범위(`CommandRunScope`) · 수명(`LifetimeScope` · `PresentationScopeSession`) · 팩토리 합성(`CompositeCommandFactory`) |

어셈블리 `Ked.CommandExecutor` — 참조: DOTween.

## 원본과 사본

이 저장소가 **원본**이다. 분리본 둘(ked-command-executor, ked-stage-runtime 안의 한 벌)을 이쪽에 맞춘다 — `Documentation~/vendoring.md`.
