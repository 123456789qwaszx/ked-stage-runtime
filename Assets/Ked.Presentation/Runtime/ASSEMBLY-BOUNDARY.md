# 어셈블리 경계 (U13-a, 2026-10-26 Playback 분리 · 2026-10-27 세 저장소)

> **세 저장소.** 이 폴더(`Assets/Ked.Presentation/`)의 원본은 [ked-stage-runtime](https://github.com/123456789qwaszx/ked-stage-runtime)이고,
> 실행기(`Assets/Ked.CommandExecutor/`)의 원본은 [ked-command-executor](https://github.com/123456789qwaszx/ked-command-executor)다.
> `Ked.Presentation.Playback`과 예제는 재생 저장소([ked-presentation-runtime](https://github.com/123456789qwaszx/ked-presentation-runtime))에만 있다.
> 이 문서는 세 조각이 한 프로젝트에 모였을 때의 경계를 적는다 — 어느 저장소에서 읽든 같은 문서다(복사 반입, 각 폴더의 `Documentation~/vendoring.md`).

이 폴더(`Assets/Ked.Presentation/Runtime/`)는 **하나의 어셈블리**다. 왜 이렇게 생겼는지를 여기 남긴다.
경계는 코드 어디에도 안 보이고 `.asmdef` 파일 안에만 있어서, 근거가 없으면 다음 사람이
"왜 폴더가 이렇게 묶여 있지"를 알 수 없다.

**목적**: `Ked.Presentation.Core`(순수 C# 상태 계산 층)를 뽑아 외부 도구(VnTool)가
참조할 수 있게 하는 것. U13-a에서 경계를 긋고, **U13-b에서 실제로 뽑았다** —
지금은 VnTool에 `src/Ked.Presentation.Core`로 복사 반입돼 있다.

---

## 1. 어셈블리

| 어셈블리 | 위치 | 내용 |
|---|---|---|
| `Ked.CommandExecutor` | `Assets/Ked.CommandExecutor/` | **실행기** — 커맨드 수명(`CommandBase` · `ClaimTweenCommandBase`) · `CommandRunScope` · `CommandExecutor` · `SequencePlayer` · 포트 (참조: DOTween.Modules. **무대·재생·Yarn을 모른다**) |
| `Ked.Presentation.Core` | `Assets/Ked.Presentation/Core/` | 순수 상태 계산 — 정지 프레임 리듀스·이징·튜닝 (**엔진 의존 0**, 참조 0) |
| `Ked.Presentation.Runtime` | `Assets/Ked.Presentation/Runtime/` | **무대** 실행 계층 — Stage(무대 상태·무대 키·깊이 칸 계약) · Commands(커맨드 · 팩토리 · 토큰 파서 · **StageCommandParser**) · CharacterRig(배경 = 캐스팅하지 않은 슬롯) · ShotResponse · ScreenEffect · StageMask · Audio (참조: Core · CommandExecutor · DOTween.Modules) |
| `Ked.Presentation.Playback` | `Assets/Ked.Presentation/Playback/` | 대사 재생 루프와 연출 기억 — 줄 진행·선택지(DialoguePresenter) · 재생 제어·롤백 기록·백로그·VN 재생 상태(FeatureController) · Yarn 등록·전달(YarnBridge) · 재생 호스트 계약·재생 단위(Host) · 연출 기억과 그 입구 `PlaybackMemory`(Memory) · 등가성 하네스(Diagnostics) (참조: Core · Runtime · CommandExecutor · YarnSpinner.Unity · TextMeshPro · UnityEngine.UI · DOTween.Modules) |
| `Ked.Progression` · `Ked.Progression.Save` | `Assets/Ked.Progression/` | 진행 판정 코어와 진행 저장 (**엔진 의존 0**. 원본은 형제 저장소 `ked-progression-runtime`, [vendoring.md](../../Ked.Progression/Documentation~/vendoring.md)) |
| `Ked.Presentation.Editor` (에디터 전용) | `Assets/Ked.Presentation/Editor/` | 무대 에디터 도구 — 초상 DB 빌더 · 튜닝 덤프 익스포터 · 이징 골든 (참조: Core · Runtime · CommandExecutor · UnityEngine.UI · DOTween.Modules) |
| `Assembly-CSharp` (기본) | `Assets/Samples/Scripts/` | **예제 층** — Game(부트스트랩·조립·저장 흐름) · UI · ProgressionHost(진행 런타임을 꽂는 어댑터) |
| `Assembly-CSharp-Editor` (기본) | `Assets/Samples/Editor/` | 예제 쪽 도구 — 폰트 교체 |
| `Ked.Presentation.Core.Tests` | `Assets/Ked.Presentation/Core/Tests/EditMode/` | EditMode 테스트 (참조: Core · Runtime) |

> **에디터 도구는 런타임 폴더 안에 두지 않는다.** 한때 런타임 트리 안에 에디터 전용
> 어셈블리가 하나 더 있었으나, 그러면 폴더를 읽는 사람이 매번 "이건 빌드에 들어가나"를
> 확인해야 한다. 본체 뿌리의 `Assets/Ked.Presentation/Editor/`로 모으면 **위치가 곧 답이다.**
> 그 폴더는 에디터 전용 어셈블리라 예제 타입을 쓰면 컴파일이 깨진다 — 씬에서는 본체 계약
> (`IStageMaskProvider` · `IRigPrefabSource`)으로만 찾는다.

**의존 방향은 한쪽뿐이다.**

```
Assembly-CSharp (예제) ──> Ked.Presentation.Playback ──> Ked.Presentation.Runtime ──> Ked.Presentation.Core
                                   │                              │
                                   └──────────────────────────────┴──> Ked.CommandExecutor (실행기 — 밖으로 나가는 참조 0)
Ked.Presentation.Editor (에디터 전용) ────────────────────> Ked.Presentation.Runtime
        └────────────────> Ked.Progression.Save ──> Ked.Progression
        ✗ 역방향 불가 (컴파일러가 막는다)
```

연출 쪽 셋과 진행 쪽 둘은 서로 모른다. 둘을 잇는 것은 예제 층의 `ProgressionHost/`뿐이다.
순수 코어(`Ked.Presentation.Core` · `Ked.Progression` · `Ked.Progression.Save`)는 유니티도 모른다.

---

## 2. 왜 일곱 폴더가 한 어셈블리인가

두 사실이 겹친 결과다.

1. **`.asmdef`는 자기 폴더 트리를 덮지, 형제 폴더를 묶지 못한다.**
2. **코어 폴더들은 서로 순환한다** — `CharacterRig ↔ ShotResponse`(depth가 리그 값이면서 철도 층을 고르고,
   focus가 리그 체인·ScreenPoint·shot을 함께 본다), `Commands → CharacterRig · ShotResponse · …`,
   `CharacterRig → PresentationCore`(`PresentationStage`의 레지스트리).
   어셈블리 그래프는 비순환이어야 하므로 이들은 **한 어셈블리일 수밖에 없다.**

그래서 `Runtime/` 부모 폴더를 만들어 모았다. 폴더를 나누고 싶으면 먼저 폴더 간 순환을 없애야 한다.

> 2026-10-27 대조: 한때 적혀 있던 `ScreenEffect ↔ CharacterRig`·`ScreenEffect ↔ BackgroundRig` 순환은 지금 **0건**이다.
> (BackgroundRig 폴더는 2026-10-29에 없어졌다 — 배경은 캐스팅하지 않은 캐릭터 슬롯이다.)
> `CharacterRig ↔ ShotResponse`는 자연스러운 순환이라 끊지 않는다 — 억지로 끊으면 depth가 두 곳에 생긴다
> ([PLAN_stage-seam.md](../../../docs/PLAN_stage-seam.md) §3).

## 3. 대사 재생 루프는 왜 이름 있는 어셈블리가 됐나 (2026-10-26)

처음에는 대사 재생 루프(줄 진행·롤백·백로그·시크·Yarn 브리지)를 "글루"로 보고 기본 어셈블리에 두었다.
기본 어셈블리는 모든 asmdef를 자동 참조하므로 "글루 → 코어" 단방향이 공짜로 성립하고, asmdef를 씌워 얻는 것이 없었다.

롤백·백로그·시크·선택 기록이 **이 저장소의 상품**이라고 정하면서(`docs/PLAN_playback-unit.md` E1·E5) 이유가 생겼다.
예제(UI·Game·ProgressionHost)와 같은 어셈블리에 있으면 "연출이 스스로 기억하는 것"의 경계가 코드에 보이지 않고,
그 안쪽이 예제 타입을 집어도 컴파일러가 막지 않는다. 그래서 `Ked.Presentation.Playback`으로 묶었다.

기본 어셈블리에 남은 것은 **예제뿐**이다. 예제에 asmdef가 없는 이유는 처음과 같다 — "예제 → 상품" 단방향이 공짜로 선다.

옮길 때 걸린 것은 셋이었다.

- **대사창·선택지창 뷰 베이스**(`PresentationDialogueBoxViewBase`, `PresentationOptionsBoxViewBase`, `VNDefaultOptionsPanel`)는
  UI 프레임(`UIBase`)을 상속한다. `UI/DialogueBox/`에 남겼고, Playback은 그 계약(`IPresentationDialogueBoxView` · `IPresentationOptionsBoxView`)만 안다.
- **예제 조립부**(`PresentationAssembler`, `PresentationSceneRefs`)는 UI 매니저·패치 서비스를 세운다. `Game/`으로 옮겼다.
- **연출 기억 코덱**(`PresentationMemory`, 예전 `ProgressionHost/PresentationSaveState`)은 진행 저장의 JSON 설정을 빌려 쓰고 있었다.
  자기 설정을 갖게 해 진행 참조를 끊었다. 형식(camelCase)은 그대로다.

### 호스트가 보는 면 (P3)

예제 층(호스트)이 Playback에서 만지는 것은 `PresentationRuntime`의 둘뿐이다.

| | 무엇 | 호스트가 하는 말 |
|---|---|---|
| `Unit` (`PlaybackUnit`) | 재생 단위의 수명 | 열어라 · 이 노드를 재생해라 · 멈춰라 · 루트부터 다시(표적 색인을 돌려받는다) · 시크 중인가 |
| `Memory` (`PlaybackMemory`) | 연출 기억의 입구 | 꺼내라(백로그 · 단위 선택 · 재개 지점) · 되돌려라 · 과거 단위의 이 줄을 읽어라 |

문자열 형식(`PresentationMemory`), 재개 지점(`PresentationResumePoint`), 줄 좌표(`LineTarget`), 과거 단위 셈법(`BacklogForkPoints`),
무대 계약(`IPlaybackStage`)은 `internal`이다 — 호스트가 백로그·선택 기록·줄 좌표의 생김새를 집으면 **컴파일러가 막는다**.
이어하기도 따로 부르지 않는다. `Memory.Restore`가 재개 지점을 예약하면 다음에 열리는 단위가 그 줄까지 시크한다.

## 4. `YarnBridge`는 Runtime이 아니라 Playback에 있다 — 그리고 뜻은 갖지 않는다 (2026-10-27)

`YarnCommandBridge`는 Yarn 등록(이름·인자 수)과 **전달**뿐이다. Yarn이 나눠 준 토큰을 코어의
`StageCommand`(이름 + 문자열 인자)로 묶어 `Runtime/Commands/StageCommandParser/`에 넘기고, 파서가 인자 자리·생략값·
단위·커브를 풀어 `CommandSpec`을 낸다. 실행은 `Runtime/Commands/`의 커맨드와 팩토리가 한다.

대본 문법의 **뜻이 사는 곳은 파서 한 곳**이다. 등가성 하네스의 폴드(`StageReducer`)도 같은 `StageCommand`를 받으므로,
같은 줄을 읽는 파서가 둘이던 때(브리지의 스펙 파싱 · 코어의 폴드 파싱)보다 하네스가 대조할 것이 줄었다.
생략값은 `StageCommandParser.Table.cs`에만 있다 — 브리지의 전달 메서드는 전부 `= null`이다(두 곳이 되지 않게).

Runtime의 `Yarn.Unity` 참조는 여전히 **0**이다 — 코어가 Yarn을 모르는 상태가 U13-b(순수 코어 추출)의 전제다.
Yarn을 아는 것은 Playback부터다. 대사창·표면 레이아웃(`box_*`·`surface_*`)과 `debug_log`는 재생 쪽이라 브리지에 남는다.

## 5. ⚠ `partial class`는 어셈블리를 넘을 수 없다

이 작업에서 **세 번 걸린 함정**이다. 참조는 인터페이스로 끊을 수 있지만,
같은 타입의 조각이 두 어셈블리에 있으면 방법이 없다.

이 저장소는 "provider 계약과 그 구현을 한 파일에 둔다"는 관습이 있었다. 읽기엔 좋지만
경계에서는 정확히 벽이 된다 — 계약은 코어가, 구현은 UI가 가져야 하는데 한 파일이라 못 가른다.

**그래서 생긴 것이 `Assets/Samples/Scripts/UI/Providers/`다.**
UI 타입이 기능 폴더의 계약을 구현하는 partial을 전부 여기 모았다.

| UI/Providers/ | 구현하는 계약 | 계약의 위치 |
|---|---|---|
| `PresentationUIRoot.ShotResponseStage.cs` | `IShotResponseStageProvider` | `Runtime/ShotResponse/` |
| `PresentationUIRoot.StageDepthContentSlot.cs` | `IStageDepthContentSlotProvider` | `Runtime/Stage/` |
| `PresentationUIRoot.StageMask.cs` | `IStageMaskProvider` | `Runtime/StageMask/` |
| `PresentationUIRoot.StageCounts.cs` | (계약 아님 — enum을 세는 private const) | enum은 `Runtime/Stage/` |

> **새 provider를 만들 때**: 계약(interface)은 그것을 필요로 하는 기능 폴더에,
> 구현 partial은 `UI/Providers/`에. **한 파일에 같이 두지 말 것.**

## 6. 코어가 바깥을 필요로 할 때 — 포트

코어가 호스트에게 무언가 요구해야 하면 `Runtime/PresentationCore/Execution/CommandRuntimePorts.cs`에
인터페이스를 두고, 구현은 바깥 어셈블리(Playback 또는 예제)가 갖는다. 기존 예:

- `IInputSource` · `ITimeSource` · `ISignalBus` · `ISignalLatch`
- `IUIThemePatchPort` — `ui_patch` 커맨드용. 구현은 `UI/Core/Patcher/UIThemePatchAdapter.cs`
  ("현재 화면"은 `UIManager`만 알고 "패치 방법"은 `UIPatchService`만 알아서, 둘을 아는 어댑터가 필요했다)
- `ICommandPacePolicy` — 실행기가 재생 정책에서 받는 둘: 실행을 압축할지(빨리 넘기기·시크), 시간 배율.
  구현은 Playback의 `PresentationSessionContext`가 VN 재생 상태를 접어서 준다. `CommandRunScope`는 이 정책과
  무대(`PresentationStage`)만 받는다 — VN의 자동·속도·스킵·시크 깃발은 스코프에 없다(2026-10-27)

Playback이 예제에게 요구할 때도 같다. 계약은 Playback에, 구현은 예제에 —
대사창 뷰(`IPresentationDialogueBoxView`)와 화면 전환(`IPresentationView`)이 그렇다.

## 7. ⚠ DOTween 의존

`DOAnchorPos`(RectTransform) · `DOFade`(CanvasGroup) 같은 확장은 **DOTween 코어 DLL이 아니라
`Assets/Plugins/Demigiant/DOTween/Modules/`의 소스 파일**에 있다. asmdef 없이 두면 그 소스가
`Assembly-CSharp`에 컴파일되고, 코어는 기본 어셈블리를 참조할 수 없어 확장이 안 보인다.

그래서 `Modules/DOTween.Modules.asmdef`를 만들고 코어가 참조한다.

> **DOTween을 업데이트하면 이 asmdef가 지워질 수 있다.**
> 그때는 DOTween 유틸리티 패널의 "Create ASMDEF"로 다시 만들면 된다(같은 이름을 쓴다).

---

## 8. 경계 오류를 만났을 때

컴파일러가 "type or namespace not found"를 내면 대개 **경계를 넘으려 한 것**이다. 순서대로 물어보라.

1. **안쪽 어셈블리의 코드가 바깥 타입을 쓰려는가?** (Core ← Runtime ← Playback ← 예제)
   → 그 코드가 정말 안쪽 것인지 다시 본다. 아니면 바깥으로 옮긴다(`YarnBridge`, 뷰 베이스가 그랬다).
   → 안쪽이 맞다면 **포트 인터페이스**를 만든다(§6).
2. **같은 타입의 `partial`이 양쪽에 있는가?**
   → 파일을 가른다(§5). 인터페이스로는 못 푼다.
3. **확장 메서드가 안 보이는가?**
   → 그 확장이 어느 어셈블리에 있는지 본다. `Assembly-CSharp`이면 asmdef가 필요하다(§7).

## 9. `Ked.Presentation.Runtime`은 "순수 코어"가 아니다

이 어셈블리는 **의도적으로 `UnityEngine`·DOTween·`MonoBehaviour`에 의존한다** — 실행 계층이다.
순수 상태 계산은 U13-b에서 `Ked.Presentation.Core`로 뽑혀 나갔고(§1),
이 어셈블리는 그 계산을 실제 무대에 재생하는 쪽을 맡는다.

참고 수치(U13-a 착수 시점, `Assets/Scripts` 572파일 기준):

- `Commands/` 116파일 중 `MonoBehaviour`는 **1개**, `PresentationCore/` 54파일 중 **2개**
  → 엉킴은 `MonoBehaviour`가 아니라 **`RectTransform`을 상태 담지체로 쓰는 것**(402회)이다
- 커맨드 51개가 이미 "목표값 계산(`ClaimTarget`)"과 "트윈"을 나눠 두었다
  → 정지 프레임(2b)에 필요한 것은 앞쪽뿐이고, DOTween 의존은 뒤쪽에 몰려 있다
- `ShotResponse/CharacterPlacementTargetLedger.cs`가 "트윈이 끝났다면 어디 있을 것인가"를
  이미 별도 딕셔너리로 들고 있다 → `StageState`의 원형이다

---

## 10. 세 조각 — 실행기 · 무대 · 재생 (2026-10-27)

이 저장소의 관심사는 둘이 아니라 셋이다. 리포지토리를 가르기 전에 이 안에서 먼저 선을 그었다
(①②③, [PLAN_stage-seam.md](../../../docs/PLAN_stage-seam.md)).

```
재생 (Playback · 예제)  ──▶  무대 (Core · Runtime의 리그·shot·커맨드·파서)
         │                              ▲
         └──────▶  실행기 (PresentationCore의 CommandBase · ClaimTweenCommandBase · CommandRunScope · LifetimeScope · 포트)
```

- 리졸버는 `CommandRunScope`가 아니라 `PresentationStage`를 받는다 — 무대가 실행기를 모른다(①).
- 스코프는 `ICommandPacePolicy`뿐이다 — 실행기가 VN도 무대도 모른다(②·④).
- 대본 커맨드의 뜻은 `StageCommandParser`가 갖는다 — 재생은 `StageCommand`만 만든다(③).
- 무대가 필요한 커맨드는 생성자로 받고, 팩토리가 들고 넘긴다(④). `PresentationCore/`에는 실행기만 남았다 —
  무대 상태(`PresentationStage`·`PresentationStageKey`·`IStageDepthContentSlotProvider`)는 `Stage/`로, 토큰 파서는 `Commands/Parser/`로.

실행기는 그대로 저장소가 됐다 — `Assets/Ked.CommandExecutor/`(어셈블리 `Ked.CommandExecutor`, 원본 ked-command-executor).
`Ked.Presentation.Runtime`은 `PresentationCore/`가 빠지고 실행기를 참조한다. 무대(`Core` · `Runtime` · `Editor` · 데이터·프리팹·셰이더)의 원본은 ked-stage-runtime이다.
