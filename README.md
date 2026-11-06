# ked-stage-runtime

> 연출 **무대** — 캐릭터 리그 계층과 철도(shot·depth), 그 위를 움직이는 커맨드, 대본 커맨드의 뜻(파서), 그리고 "트윈이 끝났다면 어디에 있을 것인가"를 접는 순수 코어.

원본은 [ked-presentation-runtime](https://github.com/123456789qwaszx/ked-presentation-runtime)이다. 이 저장소는 그중 **무대**만 따로 보이려고 떼어 둔 분리본이다 —
실행기는 [ked-command-executor](https://github.com/123456789qwaszx/ked-command-executor)에 같은 식으로 떼어 두었다. 가른 근거와 결정은 원본의 `docs/PLAN_stage-seam.md`에 있다.

## 무엇이 있나

| 어셈블리 | 자리 | 무엇 |
|---|---|---|
| `Ked.Presentation.Core` | `Assets/Ked.Presentation/Core/` | 계산. 커맨드가 끝나면 노드가 어떤 값이어야 하는가(`NodeTargets` · `ShotIntents`), 커맨드를 정지 프레임으로 접는 리듀서, 대본 낱말(`12fr` · `2u`), 이징·모션 수학, 튜닝 형식. 순수 C#(엔진 참조 0)이라 저작 툴이 그대로 반입한다 |
| `Ked.Presentation.Stage` | `Assets/Ked.Presentation/Stage/` | 무대 실행. 기능 하나에 폴더 하나 — `Grammar`(대본 문법의 뜻, **`StageCommandParser`**) · `StageRig`(Stage00/01/02 · 깊이 층) · `Character`(캐릭터 리그 — 배경도 이 리그, 캐스팅하지 않은 슬롯) · `Shot` · `ScreenEffect` · `Audio` · `Control`. 리그 프리팹 · 셰이더 · 프리셋 기본값도 여기 있다 |
| `Ked.Presentation.Editor` | `Assets/Ked.Presentation/Editor/` | 튜닝·모션 내보내기, 초상 DB 빌더 |
| `Ked.Presentation.Tests.EditMode` | `Assets/Ked.Presentation/Tests/EditMode/` | EditMode 테스트 — 리듀서 · 이징/커브 등가 · 파서 · Unity 대조 |
| `Ked.CommandExecutor` | `Assets/Ked.CommandExecutor/` | 실행기 — 원본 ked-presentation-runtime의 한 벌([vendoring.md](Assets/Ked.CommandExecutor/Documentation~/vendoring.md)) |

폴더 하나에 어셈블리 하나다. 자세한 배치는 [`Assets/Ked.Presentation/README.md`](Assets/Ked.Presentation/README.md)(원본과 같은 파일이라 재생 층 `Playback/`도 적혀 있다 — 이 저장소에는 없다), 경계의 근거는 [`ASSEMBLY-BOUNDARY.md`](Assets/Ked.Presentation/Documentation~/ASSEMBLY-BOUNDARY.md).
그 밖에 `ExportedTuning/`(튜닝 덤프와 [schema.md](ExportedTuning/schema.md)).

## 모션

모션은 전용 툴(`ked-motion-tool`)이나 **Unity Animation 창**으로 만든다. Unity에서는 **Ked › Motion › Create Authoring Rig**로 스키마가 세운 리그를 씬에 놓고(손으로 만든 프리팹은 쓰지 않는다) 키잉하고 `MotionDefinition`(Create › Ked › Motion Definition)에 뜻(id·리그·end)을 적은 뒤
메뉴 **Ked › Motion › Export motions.json**으로 낸다. 대본은 `<<motion slot 이름 [relative|absolute]>>` — relative/absolute는 모션이 아니라 대본이 정한다(motions/2).
규약은 [docs/motion-contract.md](docs/motion-contract.md) — 파일 형식 · relative/absolute · 종료 정책(restore/commit) · 충돌 · 받는 속성.
전용 저작 도구의 계획은 [docs/PLAN_motion-tool.md](docs/PLAN_motion-tool.md).

## 무엇을 모르나

- **재생을 모른다.** 줄 진행·롤백·백로그·시크·대사창·Yarn은 재생 저장소에 있다. 여기가 받는 것은 코어의 `StageCommand`(이름 + 문자열 인자)뿐이다.
- **무대를 놓을 자리는 모른다.** 무대 리그(`StageRigBuilder` · `StageRigRegistry`)는 여기서 세우지만, 어느 캔버스 아래에 둘지(mount)는 씬을 가진 쪽(재생 저장소의 조립부)이 정한다. 에디터 도구는 `IRigPrefabSource` 계약으로만 씬의 리그 프리팹에 닿는다.

## 세 계통 — 상태 소유권

무대 안은 어셈블리가 아니라 **누가 어느 상태를 쓰는가**로 가른다. 세 계통은 서로의 상태를 쓰지 않는다(읽기는 된다).

| 계통 | 쓰는 상태 | 커맨드 |
|---|---|---|
| 철도(shot) | `StageState.Shot` · 무대 pan/zoom 루트 · 층 프레이밍 | `shot_*` |
| 열차 배치(슬롯 축) | `Track_Focus`(해) · `DepthY`·`DepthScale` · `Track*`·`Rotation`·`SwayPivot`·`Scale` | `place`·`size`·`move_by`·`left/right/up/down`·`rotate_by`·`scale_by` |
| 열차 연기(초상 축) | `CharacterPortrait_*` | `motion`·`char_rotate_to`·`char_scale_to`·`mirror`·`face`·`pose` |

리그 계층이 왜 이 순서인지는 [`rig-axis-order.md`](Assets/Ked.Presentation/Stage/Character/Rig/Documentation~/rig-axis-order.md), 목표 계산 규약은 [`target-boundary.md`](Assets/Ked.Presentation/Core/Documentation~/target-boundary.md).

## 이름에 대해

저장소는 "무대"지만 어셈블리·네임스페이스는 `Ked.Presentation.*`다. 저작 도구(VnTool)가 `Ked.Presentation.Core` 네임스페이스를 그대로 반입하고 있고, 재생 저장소의 씬·프리팹이 같은 스크립트 GUID를 가리킨다. 이름을 바꾸면 그 둘이 함께 바뀌어야 한다 — 바꿀 날이 오면 세 저장소를 한 번에.

## 원본과의 관계

패키지가 아니다. 이 저장소는 원본(ked-presentation-runtime)의 `Assets/Ked.Presentation/`(Playback 제외)과 `ExportedTuning/`을 `.meta`까지 같은 한 벌로 따라간다.
고치는 곳은 원본이고, 고친 뒤 이쪽을 맞춘다. 지금 어느 판인지는 원본의 `Assets/Ked.Presentation/Documentation~/vendoring.md`에 적는다.

## 의존

- Unity `6000.3.16f1` · URP (`Assets/Settings`)
- DOTween (`Assets/Plugins/Demigiant/`)
- 실행기 `Ked.CommandExecutor`(원본의 한 벌)
