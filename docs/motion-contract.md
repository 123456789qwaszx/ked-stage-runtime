# 모션 규약 — motions/1

모션은 **리그 노드 몇 개의 시간별 상태**다. 이징 곡선(`curves.json`)이 이동 명령의 진행률 **모양**이라면, 모션은 **내용**이다.
둘은 역할이 다르므로 파일도 따로다. 이 문서가 `motions.json`의 규범이고, 코드의 규칙(`Core/Motion/MotionRules.cs`)은 이 문서의 구현이다.

## 0. 누가 무엇을 소유하나

| | 소유 | 비고 |
|---|---|---|
| 규약·검증·샘플링 수학 | **Core** (`Motion/`) | 순수 C#. 내보내기·로더·저작 도구(VnTool)가 같은 함수를 쓴다 — "툴에선 되는데 재생에서 사라지는" 모션이 없다 |
| 제작 원본 | **AnimationClip + MotionDefinition** (Unity 에셋) | Animation 창에서 키잉하고, 뜻(id·리그·end·mode)은 `MotionDefinition`에 적는다. 클립 이름에 뜻을 숨기지 않는다 |
| 산출물 | **motions.json** | 원본을 변환한 것. **손으로 고치지 않는다.** 게임 콘텐츠라 대본 번들 곁(`curves.json` 옆)에 산다 |
| 제작 리그 | `Editor/MotionAuthoringRig` (메뉴 Ked/Motion/Create Authoring Rig) | 재생이 쓰는 `CharacterRigBuilder`가 **스키마로** 세운다 — 손으로 만든 프리팹을 쓰지 않는다. 기준 해상도 캔버스 아래, 초상 알파 1 |
| 내보내기 | `Editor/MotionClipExporter` | 의존은 **Core + rig-schemas.json**뿐이다(런타임 스키마 코드를 모른다). 노드는 끝 이름이 아니라 **부모 사슬 전체**를 대조한다 — 계층이 다른 리그에서 만든 클립은 거부. 언젠가 따로 떼어도 코드가 안 바뀐다 |
| 재생·정지 프레임 | `Runtime/MotionCommandCharR` · `Core/StageReducer`(`motion`) | 같은 `MotionMath`를 지난다 |
| 대본 | VnTool | 이름만 쓴다: `<<motion slot 이름>>`. 어디로·언제는 VnTool, 어떻게는 모션 |

의존 방향: 모션 툴 → Core · 데이터. 무대 런타임 → Core. **무대 런타임은 모션 툴을 모르고, 모션 툴은 재생·VnTool을 모른다.**

## 1. 파일

```json
{
  "schema": "motions/1",
  "motions": [
    { "name": "nod", "rig": "character", "duration": 0.6, "end": "restore",
      "channels": [
        { "node": "CharacterPortrait_Track_Move_Y", "kind": "position", "mode": "relative",
          "y": [ { "t": 0, "v": 0, "inTangent": 0, "outTangent": -120 },
                 { "t": 0.3, "v": -18, "inTangent": 0, "outTangent": 0 },
                 { "t": 0.6, "v": 0, "inTangent": 60, "outTangent": 0 } ] }
      ] }
  ]
}
```

| 필드 | 뜻 | 규칙 |
|---|---|---|
| `name` | 대본 토큰 | `[a-z0-9_]+`, 파일 안에서 유일 |
| `rig` | `rig-schemas.json`의 `rigKind` | 채널 노드를 이 리그로 검증한다. 지금은 `character`뿐 |
| `duration` | 초 | `> 0`. 모든 채널의 모든 성분이 이 시각에 마지막 키를 둔다 |
| `end` | 끝난 뒤 무엇이 남는가 | `restore` \| `commit`. **필수, 기본값 없음** (§3) |
| `channels[]` | 노드 하나의 속성 하나 | 하나 이상. `(node, kind)`는 한 모션 안에서 유일 |
| `node` | 리그 스키마의 노드 id | 스키마에 있어야 한다. `CharSlot_Track_Focus`는 **금지**(저작값이 아니라 해(解)다 — focus × ScreenPoint × 정착 체인에서 풀린다) |
| `kind` | `position` \| `scale` \| `euler` | anchoredPosition(px) · localScale(배율) · localEulerAngles(도) |
| `mode` | `relative` \| `absolute` | **필수, 기본값 없음** (§2) |
| `x` `y` `z` | 성분별 키 | 생략 = 건드리지 않는다. `z`는 `euler`에만. 키 2개 이상, `t` 오름차순, 첫 키 `t=0`, 마지막 키 `t=duration` |
| 키 | `t` 초 · `v` 값 · `inTangent` · `outTangent` | Unity `Keyframe`과 같은 Hermite. 가중 탄젠트·무한 탄젠트 없음 |

Transform 경로는 파일에 없다. 내보내기가 경로를 노드 id로 바꾸되, 부모 사슬이 `rig-schemas.json`과 마디마다 같아야 한다(접두사는 허용). 리그 내부 계층이 바뀌면 덤프를 다시 내고 모션도 다시 내보낸다 — 조용히 맞는 척하지 않는다.

## 2. relative와 absolute

| | 뜻 | 중립값 | 첫 키 |
|---|---|---|---|
| `relative` 위치·회전 | `start + v` (덧셈) | 0 | 0이어야 한다 — 아니면 첫 프레임이 튄다 |
| `relative` 배율 | `start × v` (곱셈) | 1 | 1이어야 한다 |
| `absolute` | `v` 그 자체. 없는 성분은 start 유지 | — | 제한 없음(첫 프레임의 스냅은 작가의 선택) |

배치 축(`CharSlot_*`)은 "지금 자리에서"가 자연스럽고(relative), 연기 축(`CharacterPortrait_*`)은 "이 포즈로"가 자연스럽다(absolute).
제작 에셋(`MotionDefinition`)은 모션 단위 기본 mode + 채널 재정의를 허용하지만, **파일에는 채널마다 명시**돼 나간다.

## 3. 종료 정책

| | 남는 것 | 규칙 | 정지 프레임 | 장부 |
|---|---|---|---|---|
| `restore` | 없음 — 흔들기·끄덕임 | 모든 채널 `relative` **그리고** 마지막 키가 중립값. absolute 금지(끝에서 튄다) | 무변(gesture와 같다) | 게시 없음 |
| `commit` | 마지막 키 — 고개 숙인 채로, 자세 바꾸기 | 제한 없음 | 채널마다 **마지막 키**를 클레임으로 접는다 | 시작 시 종점 게시 |

종점은 평가 결과가 아니라 **키 값**이다. 그래서 리듀서는 샘플링하지 않고, "이징은 종점에 관여하지 않는다"가 모션에서도 선다.
중립값으로 끝나는 채널은 `commit`이라도 아무것도 남기지 않으므로, "어깨는 처진 채로 남고 고개는 흔들다 돌아오는" 모션은 `commit`이다.
`restore`는 "아무것도 남기지 않는다"는 **보증**이고 내보내기가 검증한다 — 선언과 키가 어긋나면 거부.

재생은 매 프레임 **시작 상태 + 현재 시각**으로 계산한다(`MotionMath.Compose`). 누적하지 않으므로 어느 시각으로든 바로 갈 수 있다.

## 4. 충돌

한 노드 = 한 축. 같은 노드를 두 커맨드가 잡으면 뒤의 것이 `DOKill(true)`로 앞의 것을 **완주**시키고 시작한다 —
`move_by`·`gesture`와 같은 규약이다. 모션은 노드마다 트윈 하나라 이 규약을 그대로 탄다. 모션 둘이 같은 노드를 키잉해도 같다.
Skip·스텝 경계에서는 즉시 종점(commit)이나 시작값(restore)으로 확정한다. 가속은 없다 — 초 단위 진행이라 "남은 거리"가 없다.

## 5. 내보내기가 받는 것 (v1)

| 속성 | | |
|---|---|---|
| Rect Transform › Anchored Position x · y | 받음 | `position` |
| Scale x · y | 받음 | `scale` (z 없음) |
| Rotation x · y · z (Euler) | 받음 | `euler` — Animation 창의 회전 보간을 **Euler Angles**로 |
| Position(localPosition) | 거부 | 리그 노드는 anchoredPosition으로 움직인다 |
| 쿼터니언 회전(`m_LocalRotation`) | 거부 | 보간을 Euler Angles로 바꿀 것 |
| 가중 탄젠트 · 계단(Constant) 탄젠트 | 거부 | `CurveKey`에 가중이 없고, JSON에 Infinity가 없다 |
| 스프라이트 교체 · Animation Event · 그 밖의 컴포넌트 속성 | 거부 | 모션은 리그 노드의 변환뿐이다 |

받지 않는 것이 클립에 있으면 **오류**다. 조용히 무시하지 않는다. 사유는 전부 모아 한 번에 돌려준다.
`AnimationCurve.Evaluate`와 `CurveFunctions.Evaluate`의 등가는 위 속성·조건 안에서만 주장한다(`CurveFunctionsAnimationCurveParityTests` · `MotionClipExporterUnityParityTests`).

## 6. 읽기

무대는 `MotionLibrary.Parse(json, label, rigNodeIds)`만 둔다. 본문을 어디서 가져오는지(TextAsset · 경로)는 호스트가 정한다.
빌드에서는 씬이 직접 참조하는 TextAsset이 가장 단순하다 — 재생 저장소의 `SampleBootstrap › 작가 자산`.
