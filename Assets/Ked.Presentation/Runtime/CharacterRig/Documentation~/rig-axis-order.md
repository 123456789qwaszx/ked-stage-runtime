# 리그 축 순서 — 계층이 규약인 이유

`CharacterRigSchema.Nodes`의 25줄짜리 배열은 **순서만 보여주고 순서의 이유는 보여주지 않는다.**
트랜스폼 계층은 결과만 남고 의도가 안 남는 구조라, 이유를 여기 남긴다.

## 0. 두 문장

> **리그 계층은 프리팹이 아니라 스키마가 소유한다.** 프리팹은 손으로 만지면 조용히 망가지고,
> 망가진 계층은 에러가 아니라 "연출이 좀 이상하다"로 나타난다.
>
> **노드의 자리가 그 노드의 뜻이다.** 같은 연산(스케일·회전·이동)이 계층 여러 곳에 나오고,
> 무엇에 곱해지느냐가 자리로 정해진다. 자리를 옮기면 뜻이 바뀐다.

---

## 1. 기원 — 왜 프리팹이 아니라 스키마인가

### 1.0 목적이 먼저다 — 연출은 쌓아서 재활용하는 자산이다

이 시스템은 커맨드 조합으로 **연출 자산을 쌓아 올려 재사용하는 것**이 목적이다.
한 장면에서 만든 연출이 다른 화면에서, 다른 프로젝트에서 같은 결과를 내야 자산이 된다.

그러려면 **연출 자체의 균일성**이 전제다. 리그 계층이 화면마다·프로젝트마다 조금씩
다르면 같은 커맨드 조합이 다른 결과를 내고, 쌓아 온 연출은 그 순간 재사용할 수 없는
일회용이 된다.

**프리팹은 이 전제를 지킬 수 없다.** 프리팹은 씬·프로젝트마다 사본이 생기고, 사본은
따로 편집되고, 편집된 것끼리 조용히 갈린다. 그래서 계층의 소유권을 **어디서 실행되든
같은 하나뿐인 것** — 코드의 스키마로 옮겼다.

아래 1.1~1.4는 이 결론에 도달하기까지 실제로 치른 값이다.

### 1.1 프리팹이 소유하던 시절의 비용

처음(`aa81d38e`, 2026-03-09)에는 계층이 프리팹에 있었다. 코드는 프리팹을 `Instantiate`하고
이름으로 노드를 찾아 참조를 채웠다. 그때 치른 비용이 커밋 로그에 그대로 남아 있다 —
**"프리팹 최신화"가 붙은 커밋이 7회**다.

```
2026-05-19  fix: CharacterRig 프리팹 최신 버전으로 갱신
2026-06-13  chore: characterRig, BackgroundRig 내 framing계층 제거, 프리팹최신화
2026-06-15  fix: characterRig Emoji계층구조 변경 및 프리팹 최신화
2026-06-23  fix: BackgroundRig 계층 구조 수정 및 프리팹 최신화
2026-06-25  refactor: PresentationUIRoot 정리 및 프리팹 최신화
2026-08-19  refactor: Blur 레거시 코드 제거, 프리팹 최신화
2026-08-20  chore: 커맨드 및 리그 프리팹 구조 Json 갱신
```

계층을 한 번 고칠 때마다 **코드와 프리팹 두 곳을 같이 고쳐야 했다.** 그리고 프리팹 쪽을
빠뜨리면 컴파일은 통과한다. 문제는 실행 중에 좌표계가 어긋난 채로 그냥 돌아간다는 것이다.

> **두 곳에 있으면 갈린다** — 저장소 전체가 쓰는 원칙이 여기에도 그대로 적용된다.
> ([SCOPE-BOUNDARY.md](../../../../../SCOPE-BOUNDARY.md) §2)

### 1.2 결정적 하루 — 2026-05-15

소유권을 옮긴 작업이 하루에 다섯 커밋으로 끝났다. 순서에 논리가 있다:

| 커밋 | 한 일 | 왜 이 순서인가 |
|---|---|---|
| `3ece3d90` | 생성과 바인딩 책임 분리 | 옮기기 전에 **무엇이 옮겨갈지**를 먼저 갈랐다 |
| `ad3f6e7b` | `CharRig`·`CharRigTarget`·`CharRigRefs`를 한 스크립트로 통합 | 흩어진 정의를 **한 자리에 모았다** |
| `67e9aea6` | Builder 검증·복구 기능 보완 | 스키마가 소유하려면 **어긋남을 감지할 수 있어야** 한다 |
| `97dfc62c` | **graph definition을 schema로 이동** | 소유권 이전 |
| `b470fc88` | 훼손된 프리팹에서도 재생성되도록 수정 | 이전을 **실전에서 검증** |

마지막 `b470fc88`이 이 설계의 성격을 가장 잘 보여준다. 커밋 본문 전문:

```
- 증상: 훼손된 Prefab 사용시 CharacterRig가 재생성되지 않음
- 원인: Destroy()로 삭제 예약된 child가 같은 프레임 동안 hierarchy에 남아 있어,
        EnsureGraph()의 FindByName()이 삭제 예정 노드를 재사용
- 수정: Destroy() 전에 child.SetParent(null, false)로 rigRoot에서 분리해,
        재생성 과정의 검색 대상에서 삭제 예정 노드를 제거
```

`CharacterRigBuilder.EnsureValidGraphMap`에서 **파괴 전에 `SetParent(null, false)`를 부르는
줄은 장식이 아니다.** 지우면 훼손된 프리팹의 복구가 조용히 실패한다.

### 1.3 스키마 방식이 다른 리그로 퍼졌다

계약이 좋으면 전파된다. `821f1e2b`(2026-06-24)에서 **화면 효과 리그도 같은 모양으로 갈렸다**
(`refactor: screen effects를 schema기반 rig로 분리`). 지금은 네 리그가 전부 같은 네 이름을
쓴다 — `Definition` · `Builder` · `Registry` · `Resolver`.

### 1.4 그리고 계층이 데이터가 됐다 — 2026-08-08

스키마가 코드에 있으면 **런타임만 안다.** 순수 코어와 저작 툴은 못 본다. 그래서 계층을
JSON으로 내보내고 코어가 그것을 읽게 했다. 사흘 만에:

| 커밋 | |
|---|---|
| `a4b879e2` | Unity의 RectTransform 계층 좌표 변환을 Core로 포팅 |
| `597ea9e7` | Core에 RectTransform 계층을 표현하는 상태 트리 제작 (`RectNodeTree`) |
| `90ad838c` | 노드 계층 JSON을 `RectNodeTree`로 변환 |
| `eb66649a` | **계층 생성 테스트 및 검증** (`RigSchemaTreeUnityParityTests`) |

이걸로 계층의 단일 출처가 네 세계를 관통하게 됐다:

```
CharacterRigSchema.Nodes            코드      ← 유일한 소유자
        ↓ CharacterRigBuilder
    씬의 실제 계층                   Unity
        ↓ PresentationTuningExporter
  ExportedTuning/rig-schemas.json    데이터
        ↓ RigSchemaLoader
  StageReducer의 slot 폴드           순수 코어
```

**손으로 미러링한 구간이 하나도 없다.** 전부 코드가 뽑는다. 그래서 갈릴 수가 없고,
그래도 갈리는지 `RigSchemaTreeUnityParityTests`가 지킨다:

* `실제_리그에_있는_노드가_덤프에서_빠지지_않았다`
* `덤프의_measuredRectSize가_트리_계산과_일치한다`

---

## 2. 이 구조가 보장하는 것 둘

### 2.1 동작 균일성 — 커맨드가 노드의 존재를 전제할 수 있다

`CharacterRigBuilder`는 프리팹을 받아도 **믿지 않는다.** 그래프를 검사한다:

```
노드 수가 맞나 → 각 노드가 존재하나 → 부모가 스키마와 일치하나
     하나라도 틀리면 → 자식 전체 파괴 → 스키마로 재구축
```

부모까지 대조하는 게 핵심이다. 노드가 다 있어도 **하나가 엉뚱한 부모 밑에 있으면 좌표계가
통째로 어긋난다.** 그리고 그건 에러를 안 낸다.

이 검사가 통과한 뒤에는 모든 리그가 **정확히 같은 모양**이므로, 커맨드는
`refs.CharSlot_Track_X`가 있는지 묻지 않고 그냥 쓴다. 리그마다 다른 예외 경로가 없다.

경고 메시지가 원인 후보를 셋 다 적는 이유도 여기 있다 —
`broken, outdated, or saved with another role prefix`. 셋 다 실제로 겪은 것이다.

`rolePrefix`는 하나로 세 가지를 푼다:
노드 이름이 전역 유일해져 `FindByName`이 안전하고, 하이어라키에서 누가 누군지 보이고,
등가성 하네스가 **노드 키로 대조**할 수 있다(슬롯 키가 코어 노드 키의 prefix가 된다).

### 2.2 연출 항등성 — 같은 yarn은 같은 화면이 된다

계층이 리그마다·실행마다 같아야 "같은 대본 → 같은 결과"가 성립한다. 계층이 흔들리면
`StageNodeClaim`의 세 갈래가 **서로 다른 좌표계 위에서** 같은 값을 보게 되고, 그러면
같은 값인데 다른 화면이 나온다.

```
              StageNodeClaim
   ┌────────────────┼────────────────┐
① 장부 게시      ② 트윈 종점      ③ 상태 폴드
(정착 예약)      (실제 재생)      (정지 프레임)
```

세 갈래가 **같은 계층 위에 있다는 전제**가 스키마다. 코어의 폴드가 `rig-schemas.json`으로
리그를 세우는 것이 이 전제를 데이터로 못박은 것이고, 그래서 등가성 하네스의 판정이
의미를 갖는다.

> 리덕션 쪽 규약은 [reduction-boundary.md](../../../Core/Documentation~/reduction-boundary.md).

---

## 3. 축 순서 — 자리가 뜻을 정한다

### 3.1 왜 노드를 잘게 나누는가

**한 노드 = 한 축이다.** 같은 프로퍼티를 두 트윈이 잡으면 서로를 덮으므로, 동시에 돌 수
있는 축은 반드시 다른 노드로 나눈다.

`Track_X`와 `Track_Y`가 따로인 이유가 이것이다. 합치면 좌우 이동 트윈과 상하 이동 트윈이
같은 `anchoredPosition`을 두고 싸운다. 나눠 놓으면 **두 트윈이 물리적으로 다른 대상을 잡으므로
충돌이 불가능**해진다. `ActingScale_X`/`_Y`, `Track_Move_X`/`_Y`도 같은 이유다.

그래서 25단은 "깊은 계층"이 아니라 **동시에 살아 있을 수 있는 연출 축의 개수**다.

### 3.2 전체 계층과 커맨드 대응

```
CharSlot_Track_Focus          place 계열          ← 슬롯 축 (무대 배치)
 └ CharSlot_DepthY            size 계열 (Y 오프셋)
    └ CharSlot_DepthScale     size 계열 (배율)      [bottom pivot]
       └ CharSlot_Track_Idle  (idle 연출)
          └ CharSlot_Track    move_by · slide_in/out
             └ CharSlot_Track_X    left · right
                └ CharSlot_Track_Y up · down
                   └ CharSlot_Rotation
                      └ CharSlot_SwayPivot   rotate_by · rotate_reset   [bottom pivot]
                         └ CharSlot_Scale    scale_by · scale_reset     [bottom pivot]
                            │
                            └ CharacterPortrait_VisualOffset   set_anchor · show   ← 배역 축 [bottom pivot]
                               │
                               └ CharacterPortrait_Track        ← 초상 연기 축
                                  └ ..._Rotation
                                     └ ..._Track_Move
                                        └ ..._Track_Move_X
                                           └ ..._Track_Move_Y
                                              └ ..._SwayPivot   char_rotate_to   [bottom pivot]
                                                 └ ..._Shake    gesture
                                                    └ ..._ActingScale     char_scale_to
                                                       └ ..._ActingScale_X   mirror
                                                          └ ..._ActingScale_Y
                                                             ├ Sprite_Root  fade_in/out  [CG α=0]
                                                             │  └ Sprite_Image   show · face · pose
                                                             └ Overlay_Root  face_swap   [CG α=0]
                                                                └ Overlay_Image
```

### 3.3 스케일이 셋인 이유

같은 곱셈인데 **무엇에 곱해져야 하는가**가 달라서 자리가 다르다.

| 노드 | 자리 | 커맨드 | 아래에 곱해지는 것 |
|---|---|---|---|
| `CharSlot_DepthScale` | 최상단 | `size` 계열 | **전부** — 이동·넛지·idle·연기·몸짓 |
| `CharSlot_Scale` | 슬롯 축 바닥 | `scale_by` | 연기 축 전체 — **화면상 이동은 그대로, 연기 진폭만 커진다** |
| `CharacterPortrait_ActingScale` | 최하단 | `char_scale_to` | **스프라이트만** — 이동량 불변 |

**`DepthScale`이 위에 있는 것이 원근의 전부다.** 아래 전부에 곱해지므로, 멀리 있는 캐릭터는
크기만 작아지는 게 아니라 **이동량도 줄고 몸짓 진폭도 준다.** `<<left 1u>>`가 far에서는
1u만큼 화면을 못 간다. 이래야 "저 사람은 멀리 있다"가 성립한다.

반대로 **`ActingScale`이 맨 아래인 것도 의도다.** 연기용 확대가 이동량까지 키우면
"크게 그려진 채로 크게 움직이는" 것이 되어 연기가 아니라 확대가 된다.

깊이 배율의 진실은 프리셋 표가 아니라 `presets/depth.json`의 **`level` 커브** 한 장이다
(`DepthLevelLabels` 참조 — 라벨은 커브 위의 눈금일 뿐이다). far→close로 단조증가한다.

### 3.4 회전이 둘인 이유

| 노드 | 자리 | 성질 |
|---|---|---|
| `CharSlot_Rotation` | `Track_X/Y`의 **아래** | 회전해도 슬롯 이동 방향은 무대 기준 그대로 |
| `CharacterPortrait_Rotation` | `Track_Move`의 **위** | 기울면 연기 움직임이 기울기를 **따라간다** |

무대 배치에서는 회전이 이동에 새면 안 된다 — "오른쪽으로 100" 했는데 캐릭터가 기울어
있다고 비스듬히 가면 배치가 무너진다. 그래서 `Track`을 위, `Rotation`을 아래에 뒀다.

연기에서는 정반대다. 몸이 기울었으면 그 위에서 일어나는 흔들림도 같이 기울어야 실제
몸처럼 보인다. 그래서 `Rotation`을 위, `Track_Move`를 아래에 뒀다.

**같은 회전인데 요구가 반대라 자리가 반대다.**

### 3.5 이동도 둘로 갈라져 있다

초상 축의 이동이 `CharacterPortrait_Track`(회전 **위**)과 `Track_Move`(회전 **아래**)로
나뉜다. 위쪽은 캐릭터별 기본 보정(`ApplyTrackOffset`)이라 기울기와 무관해야 하고,
아래쪽은 연기라 기울기를 따라야 한다. 3.4의 규칙을 이동에도 적용한 것이다.

### 3.6 나머지 자리들

**`Track_Idle`이 `DepthScale` 아래, `Track` 위** — idle 흔들림이 깊이에 따라 줄어들면서
(멀면 덜 흔들린다), 별도 노드라 `move_by`와 싸우지 않는다. 자리 하나로 두 성질을 얻는다.

**`VisualOffset`이 두 축의 이음매** — 슬롯 축 끝, 연기 축 시작. 스프라이트마다 그려진
중심이 다른 것을 보정하는 자리이고, 연기 **위**에 있어서 연기가 보정된 위치를 기준으로
일어난다. `set_anchor`/`show`가 여기를 잡는다.

**bottom pivot 다섯 곳** (`DepthScale` · `CharSlot_SwayPivot` · `CharSlot_Scale` ·
`VisualOffset` · `Portrait_SwayPivot`) — 서 있는 인물의 기준점은 발이다. 깊이로 작아져도,
흔들려도, 커져도 발이 땅에 붙어 있어야 한다. `NeedsBottomPivot`이 스키마의 필드인 이유.

**`Sprite_Root` / `Overlay_Root`가 형제이고 둘 다 초기 α=0** — `face_swap`이 위 층을
페이드로 겹쳐 표정을 바꾼다. 등장 전에는 둘 다 안 보인다.

---

## 4. Depth를 위에 둔 대가 — `Track_Focus`가 밖에 있는 이유

원근이 계층에서 공짜로 나오는데, **공짜가 아닌 곳이 하나 있다.**

`CharSlot_Track_Focus`가 `DepthY`/`DepthScale`보다 **위**에 있다. `place` 계열은
"이 캐릭터의 얼굴을 화면 좌측 지점에" 같은 **화면 절대 좌표**를 겨누는데, depth 아래에
있으면 depth가 바뀔 때마다 그 좌표가 배율만큼 어긋나기 때문이다.

그래서 focus 축만 계층 밖으로 빼고, **depth 보정을 수식이 지게 했다.** 그 대가가:

* `SettledFocusMath.SolveFocusPlacement` — 계층이 안 해주는 보정을 손으로
* `SetDepth`의 "focus 보존" 경로 — 깊이가 변해도 focus 지점을 유지
* `CharacterRigRefs.SettledDepthScale` — 트윈이 도는 중에도 "끝나면 어느 배율인가"

마지막 것이 특히 이 대가의 증거다. 트윈 중에는 현재 스케일이 계속 변하므로, 착란원
계산이 현재값을 읽으면 매 프레임 흔들린다. **정지 프레임의 배율을 따로 들고 있어야** 한다.

> `SettledFocusMath`와 `SettledDepthScale`이 왜 존재하는지는 그 파일들만 봐서는 안 나온다.
> **이유가 이 계층에 있다.**

---

## 5. 합치면 안 되는 것

이름이 비슷해서 중복으로 보이는 쌍들이다. **전부 다른 노드여야 한다.**

| 합치고 싶어지는 것 | 합치면 생기는 일 |
|---|---|
| `CharSlot_Scale` + `ActingScale` | 연기 확대가 이동량을 키운다 — 연기가 확대로 보인다 |
| `DepthScale`을 아래로 | **원근이 사라진다.** 멀리 있는 캐릭터가 가까운 것만큼 크게 움직인다 |
| `CharSlot_Rotation` + `Portrait_Rotation` | 슬롯 회전이 이동 방향을 비틀거나, 연기가 몸 기울기를 무시한다 |
| `Track_X` + `Track_Y` | 좌우 트윈과 상하 트윈이 서로를 덮는다 |
| `Track_Idle`을 `Track`에 | idle 루프가 `move_by`를 계속 덮어쓴다 |

**이 사고는 전부 "에러"가 아니라 "연출이 좀 밋밋하다"로 나타난다.** 컴파일도 되고
예외도 안 나고 캐릭터도 잘 움직인다. 그래서 원인 추적이 거의 불가능하다.
노드를 줄이고 싶어지면 이 표를 먼저 볼 것.

---

## 6. 노드를 추가·이동할 때

### 6.1 순서를 정하는 질문

새 노드의 자리는 취향이 아니라 두 질문으로 정해진다:

1. **이 노드의 값이 무엇에 곱해져야 하는가?** — 곱해질 대상이 아래로 온다.
2. **이 노드와 동시에 돌 수 있는 축이 있는가?** — 있으면 반드시 다른 노드로.

### 6.2 고쳐야 하는 자리 (현재 여섯 곳)

`CharacterRigDefinition.cs` 한 파일 안에 다섯, `CharacterRigBuilder.cs`에 하나다.

| 순 | 자리 | 무엇을 정하는가 |
|---|---|---|
| 1 | `CharacterRigSchema.Refs` enum | **계층 정의** — 이 그래프에 어떤 노드가 있는가 |
| 2 | `CharacterRigSchema.Nodes` 배열 | **빌드 정의** — 부모·컴포넌트 요구·초기 α |
| 3 | `CharacterRigTarget` enum | **커맨드 어휘** — 커맨드가 겨눌 수 있는 대상 |
| 4 | `CharacterRigRefs`의 필드 | **실행 계층** — 해석된 런타임 참조 |
| 5 | `CharacterRigRefsExtensions.GetComponent` | 3 → 4 해석 |
| 6 | `CharacterRigBuilder.BuildRefs` | 1 → 4 바인딩 |

**이 반복은 중복이 아니라 의도다.** 넷은 서로 다른 것을 정한다:

* **존재하는 노드 ≠ 겨눌 수 있는 노드.** 1과 3이 지금 25개로 겹치는 것은 우연이고,
  타입은 그 둘이 갈라질 수 있음을 표현한다. 하나로 합치면 구조용 보조 노드를 추가하는
  순간 **커맨드가 그것을 겨눌 수 있게 된다.** `CharacterRigTarget`은 API 표면이다 —
  화면 효과 리그에 이 enum이 없는 것이 그 증거다(거기서는 `ControllerKind`로 겨눈다).
* **필드 이름 = 하이어라키 오브젝트 이름.** `refs.CharSlot_Track_X`를 보고 하이어라키에서
  `<role>CharSlot_Track_X`를 이름 대조만으로 찾을 수 있다. 딕셔너리로 접으면 이 성질이
  사라지고, Unity에서 계층을 눈으로 좇는 디버깅이 어려워진다.
* **레포 전체가 같은 관용구를 쓴다.** `UIBase`의 enum 키 참조 바인딩
  (`UIRefValidation.AppendMissing<TRef> where TRef : struct, Enum`)과 같은 모양이고,
  리그 넷이 전부 `Definition` · `Builder` · `Registry` · `Resolver`로 선다.

> ⚠ **다만 넷 사이의 대응은 아직 검사되지 않는다.** `Refs`에 노드를 넣고
> `CharacterRigTarget`에 안 넣어도, `GetComponent`의 switch에서 빠뜨려도 컴파일은 통과한다.
> 경계는 유지하되 **대응만 고정하는** 테스트가 필요하다 —
> `모든_Refs가_Target으로_겨눌_수_있다` · `모든_Target이_Refs를_해석한다`.
> (`RigSchemaTreeUnityParityTests`의 `실제_리그에_있는_노드가_덤프에서_빠지지_않았다`와 같은 모양이다.)

### 6.3 고친 뒤 확인할 것

1. `PresentationTuningExporter`로 `rig-schemas.json` 재덤프 — 안 하면 코어 폴드가 옛 계층을 본다
2. `RigSchemaTreeUnityParityTests` — 덤프와 실제 리그가 같은지
3. 등가성 하네스 랩드스킵 재판정 — 리포트가 변경 전과 같은지
   (절차적 축이 안 섞이도록 랩드스킵이 기준 프로토콜이다)

---

## 참고

* [ASSEMBLY-BOUNDARY.md](../../ASSEMBLY-BOUNDARY.md) — 어셈블리 경계
* [reduction-boundary.md](../../../Core/Documentation~/reduction-boundary.md) — 스펙 → 목표 상태 규약
* [SCOPE-BOUNDARY.md](../../../../../SCOPE-BOUNDARY.md) — 저장소 경계
