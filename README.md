# ked-stage-runtime

> 연출 **무대** — 캐릭터 리그 계층과 철도(shot·depth), 그 위를 움직이는 커맨드, 대본 커맨드의 뜻(파서), 그리고 "트윈이 끝났다면 어디에 있을 것인가"를 접는 순수 코어.

`ked-presentation-runtime`에서 떼어 낸 세 조각 중 하나다 — 실행기([ked-command-executor](https://github.com/123456789qwaszx/ked-command-executor)) · **무대(여기)** · 재생([ked-presentation-runtime](https://github.com/123456789qwaszx/ked-presentation-runtime)).
가른 근거와 결정은 재생 저장소의 `docs/PLAN_stage-seam.md`에 있다.

## 무엇이 있나

| 어셈블리 | 자리 | 무엇 |
|---|---|---|
| `Ked.Presentation.Core` | `Assets/Ked.Presentation/Core/` | 순수 C#(엔진 의존 0) — 정지 프레임 리듀서 · 장부 · 클레임 · shot 의도 · 이징·커브·진동 · 튜닝 DTO · `StageCommand`. 저작 도구(VnTool)가 그대로 반입한다 |
| `Ked.Presentation.Runtime` | `Assets/Ked.Presentation/Runtime/` | Unity 실행 계층 — `Stage`(무대 상태) · `Commands`(커맨드 · 팩토리 · **`StageCommandParser`**) · `CharacterRig` · `ShotResponse` · `BackgroundRig` · `ScreenEffect` · `StageMask` · `Audio`. 경계의 근거는 [`Runtime/ASSEMBLY-BOUNDARY.md`](Assets/Ked.Presentation/Runtime/ASSEMBLY-BOUNDARY.md) |
| `Ked.Presentation.Editor` | `Assets/Ked.Presentation/Editor/` | 초상 DB 빌더 · 튜닝 덤프 익스포터 · 이징 골든 |
| `Ked.Presentation.Core.Tests` | `Assets/Ked.Presentation/Core/Tests/EditMode/` | EditMode 테스트 — 리듀서 · 이징/커브 등가 · 파서 · Unity 대조 |
| `Ked.CommandExecutor` | `Assets/Ked.CommandExecutor/` | **반입본**(원본 ked-command-executor, [vendoring.md](Assets/Ked.CommandExecutor/Documentation~/vendoring.md)) |

그 밖에 `Data/`(프리셋 DB) · `Prefabs/`(리그 셋) · `Resources/`·`Shaders/`(시각 효과) · `Audio/`(믹서) · `ExportedTuning/`(튜닝 덤프와 [schema.md](ExportedTuning/schema.md)).

## 모션

모션 툴은 **Unity Animation 창**이다. **Ked › Motion › Create Authoring Rig**로 스키마가 세운 리그를 씬에 놓고(손으로 만든 프리팹은 쓰지 않는다) 키잉하고 `MotionDefinition`(Create › Ked › Motion Definition)에 뜻(id·리그·end·mode)을 적은 뒤
메뉴 **Ked › Motion › Export motions.json**으로 낸다. 대본은 `<<motion slot 이름>>`으로 이름만 쓴다.
규약은 [docs/motion-contract.md](docs/motion-contract.md) — 파일 형식 · relative/absolute · 종료 정책(restore/commit) · 충돌 · 받는 속성.

## 무엇을 모르나

- **재생을 모른다.** 줄 진행·롤백·백로그·시크·대사창·Yarn은 재생 저장소에 있다. 여기가 받는 것은 코어의 `StageCommand`(이름 + 문자열 인자)뿐이다.
- **철도를 깔지 않는다.** 무대 계약(`IShotResponseStageProvider` · `IStageDepthContentSlotProvider` · `IStageMaskProvider` · `IRigPrefabSource`)만 둔다. 구현은 씬을 가진 쪽(재생 저장소의 `PresentationUIRoot`)이 한다.

## 세 계통 — 상태 소유권

무대 안은 어셈블리가 아니라 **누가 어느 상태를 쓰는가**로 가른다. 세 계통은 서로의 상태를 쓰지 않는다(읽기는 된다).

| 계통 | 쓰는 상태 | 커맨드 |
|---|---|---|
| 철도(shot) | `StageState.Shot` · 무대 pan/zoom 루트 · 층 프레이밍 | `shot_*` |
| 열차 배치(슬롯 축) | `Track_Focus`(해) · `DepthY`·`DepthScale` · `Track*`·`Rotation`·`SwayPivot`·`Scale` | `place`·`size`·`move_by`·`left/right/up/down`·`rotate_by`·`scale_by` |
| 열차 연기(초상 축) | `CharacterPortrait_*` | `gesture`·`char_rotate_to`·`char_scale_to`·`mirror`·`face`·`pose` |

리그 계층이 왜 이 순서인지는 [`rig-axis-order.md`](Assets/Ked.Presentation/Runtime/CharacterRig/Documentation~/rig-axis-order.md), 리덕션 규약은 [`reduction-boundary.md`](Assets/Ked.Presentation/Core/Documentation~/reduction-boundary.md).

## 이름에 대해

저장소는 "무대"지만 어셈블리·네임스페이스는 `Ked.Presentation.*`다. 저작 도구(VnTool)가 `Ked.Presentation.Core` 네임스페이스를 그대로 반입하고 있고, 재생 저장소의 씬·프리팹이 같은 스크립트 GUID를 가리킨다. 이름을 바꾸면 그 둘이 함께 바뀌어야 한다 — 바꿀 날이 오면 세 저장소를 한 번에.

## 다른 저장소로 가져가기

패키지가 아니다. **복사 반입**이다 — `Assets/Ked.Presentation/`(Playback 제외)과 `ExportedTuning/`을 `.meta`까지 통째로 복사하고, 반입한 쪽의 `Documentation~/vendoring.md`에 원본 커밋을 적는다. 여기가 원본이다. 사본에서 먼저 고쳤으면 여기에도 같은 손을 댄다.

## 의존

- Unity `6000.3.16f1` · URP (`Assets/Settings`)
- DOTween (`Assets/Plugins/Demigiant/`)
- 실행기 반입본 `Ked.CommandExecutor`
