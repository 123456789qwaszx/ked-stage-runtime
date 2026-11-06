# 무대 리그 — 샷 계층은 스키마가 세우고, 샷 식은 코어에 산다

샷(zoom · pan · focus)이 무대에 무엇을 쓰는지, 그 계층이 왜 프리팹이 아니라 코드로 서는지,
그리고 같은 식을 런타임 · 폴드 · 하네스가 어떻게 나눠 쓰는지를 남긴다. 2026-11-05 `refactor/shotRig`의 기록이고,
2026-11-06에 무대의 역할(§2.1)과 char_to 제거(§2.4)를 더했다.

## 0. 세 문장

> **샷은 숫자 셋이고, 무대의 트랜스폼은 전부 그 셋의 함수다.** 그 함수는 코어 `ShotLayoutMath` 한 곳에 있다.
>
> **무대 계층은 `StageRigSchema`가 정하고 `StageRigBuilder`가 세운다.** 모든 노드가 항등으로 서므로 중립은 `Solve(Default)` — 잴 것이 없다.
>
> **층은 이름이 아니라 깊이 값 d다.** 샷에 d만큼 다르게 반응해서 시차를 만들고, 필요할 때 선다.
>
> **같은 세계는 한 무대의 층으로, 다른 화면은 다른 무대로.** 샷은 본 무대만 움직이고, 덮는 무대의 카메라는 중립에 고정이다.

---

## 1. 왜 바꿨나

### 1.1 종전 구조

```
StageViewportLayer
  StageShot_Root (= RigSpaceRoot)          세 무대 공통의 기준 공간
    StagePan_Root                          샷이 씀 (팬)
      StageZoom_Root                       샷이 씀 (줌)
        Stage00_Root / 01 / 02             안 움직임
          StageNNMask_Root                 마스크
          StageNNDepthSystem_Root
            StageNNDepth_{Far..Close}_Root       Measure — 바인드 때 기준값을 잼
              …_FramingTransform                 샷이 씀 (층 위치)
                …_FramingScale                   샷이 씀 (층 배율)
                  …_Content                      리그가 붙는 곳
          StageNNMaskEdge_Root
```

씬에 76노드. 이름 목록은 예제 코드 `PresentationUIRoot.Refs`(73항목)에, 참조는 인터페이스 셋
(`IShotResponseStageProvider` · `IStageDepthContentSlotProvider` · `IStageMaskProvider`)에, 층 반응 식은
`PresentationResponseMath` · `PresentationCameraRootApplier` · `PresentationResponseProfile`에, 키는 enum 둘
(`PresentationStageKey` 3 · `PresentationDepthLayerKey` 5)에 흩어져 있었다.

### 1.2 비용

| 문제 | 왜 아팠나 |
|---|---|
| 경계가 프리팹에 있었다 | "카메라 아래, 층 위"라는 수학적 경계가 코드가 아니라 씬 계층으로만 존재했다. 코드를 읽어서는 왜 노드가 그 자리에 있는지 보이지 않았다 |
| 기준값을 재야 했다 | 바인더가 플레이 시작 때 `Measure` 노드의 위치 · 배율을 잼. 씬 값이 전부 항등인데도 "항등이다"를 코드가 보장하지 못해서 매번 쟀다 |
| 식이 런타임에만 있었다 | 코어 폴드는 층 반응을 몰랐다. 등가 하네스는 샷 의도(숫자 셋)만 비교하고 그 숫자가 무대에 어떻게 쓰였는지는 보지 못했다 |
| 층이 고정 칸이었다 | 층을 하나 늘리려면 씬 · enum · 이름 목록 · 프로필 표를 같이 고쳐야 했다. 별칭 표도 둘이었다(slot과 블러가 다른 별칭 집합) |
| 카메라가 하나였다 | 팬 · 줌이 세 무대 위에 있어서 무대별 카메라는 구조적으로 불가능했다 |

캐릭터 리그가 2026-05-15에 밟은 길과 같다([rig-axis-order.md](../../CharacterRig/Documentation~/rig-axis-order.md) §1) —
계층을 프리팹에서 스키마로 옮기면 노드의 존재와 항등이 코드로 보장된다.

---

## 2. 지금 계층

```
StageViewportLayer                       (IStageMount — 무대가 서는 자리. 예제의 PresentationUIRoot)
  {stage}_Stage_Root                     세상의 자리. 안 움직인다. 리그 좌표 계산의 경계(RigSpaceRoot)
    {stage}_Mask_Root                    장면 전환 마스크. 카메라 위라서 화면에 고정된다
      {stage}_Camera_Pan                 샷의 팬
        {stage}_Camera_Zoom              샷의 줌
          {stage}_Layer_{라벨|d}          깊이 층. 위치 · 배율을 이 한 노드에 쓴다. d 오름차순 = 형제 순서
            {stage}_Layer_{…}_Content    리그가 붙는 곳. 블러 대상
    {stage}_MaskEdge_Root                마스크 테두리 그래픽. 마스크에 잘리지 않도록 Mask_Root의 형제
```

| 노드 | 누가 쓰나 | 중립 | 샷 소유 |
|---|---|---|---|
| `Stage_Root` | 아무도 | 항등 | 아니다 |
| `Mask_Root` · `MaskEdge_Root` | 마스크 커맨드(`tx` 등) — 트랜스폼이 아니라 마스크 그래픽 | 항등 | 아니다 |
| `Camera_Pan` | 샷 시스템 | `anchoredPosition` (0, 0) | 그렇다 |
| `Camera_Zoom` | 샷 시스템 | `localScale` 1 | 그렇다 |
| `Layer_{d}` | 샷 시스템 | 항등 | 그렇다 |
| `Layer_{d}_Content` | 아무도(리그의 부모일 뿐) | 항등 | 아니다 |

- 노드 이름은 `{무대 키}_{노드}` — 한 씬에 무대가 셋이라 접두사로 가른다. 층 이름의 꼬리는 정본 라벨이 있으면 라벨, 없으면 d 숫자(`stage00_Layer_0.25`).
- 모든 노드는 stretch 앵커 · 피벗 중앙 · 항등으로 선다. 그래서 각 노드의 원점이 부모의 중심이고, 중립값이 상수다.
- 무대는 조립 때 셋(`stage00` · `stage01` · `stage02`)이 서고, 층 다섯(far…close)을 미리 세운다 — 먼 층이 뒤에 그려지는 순서를 처음부터 고정하려고. 그 밖의 d는 참조될 때 선다(`StageRigBuilder.EnsureLayer`, 형제 순서를 d 오름차순으로 다시 맞춘다).

### 2.1 무대마다 카메라가 있고, 역할이 그 쓰임을 정한다 (D2 · D11)

카메라가 무대 루트 **아래**로 내려왔다. 그래서 무대마다 `Camera_Pan` · `Camera_Zoom`이 있다.
어느 무대의 카메라를 샷이 움직이는지는 구조가 아니라 정책이고, 그 정책이 무대의 역할(코어 `StageRoles`)이다.

| 역할 | 무대 | 무엇을 담나 | 카메라 |
|---|---|---|---|
| 본 무대 (`Main`) | `stage00` | 장면. 같은 세계의 원근은 그 안의 층(d)이 맡는다 | 샷이 움직인다 |
| 덮는 무대 (`Overlay`) | `stage01` · `stage02` | 컷인 · 인서트 · 커튼 — 다른 화면 | 중립에 고정 |

원칙은 **같은 세계는 한 무대의 층으로, 다른 화면은 다른 무대로**다. 같은 세계의 시차는 이미 층이 맡으므로
무대끼리 카메라를 공유할 이유가 없다. 무대를 나눈다는 것은 다른 화면이라는 뜻이어야 한다.

- 역할은 무대 키로 정한다(`StageRoles.Of`). 대본에서 무대를 생략하면 본 무대다(slot · bg_spawn). 런타임 샷 시스템과 코어 하네스가 이 한 곳을 본다.
- 덮는 무대가 흔들리지 않는다. 종전(모든 무대에 같은 intent)에는 컷인이 열린 채 `shot_zoom`을 하면 패널 안의 내용까지 밀렸다.
- 덮는 무대의 클로즈업에 카메라가 필요 없다. 그 무대에 세운 리그를 `size` · `place`로 크게 세운다.
- 덮는 무대에 선 리그에 `shot_focus_to`를 걸 수 없다. 본 무대의 카메라를 다른 무대의 점으로 풀 수 없어서다 — 런타임은 경고하고 건너뛰고, 폴드는 Unhandled로 남긴다.
- 포기한 것: 덮는 무대 안의 카메라 연출(켄 번스 같은 천천히 줌). 리그의 move · scale 트윈으로 대신한다. 필요해지면 `shot_*`에 무대 인자를 단다(생략값은 본 무대).

### 2.2 마스크가 카메라 위로 올라갔다 (동작 변경)

종전에는 마스크가 줌 아래라 `shot_zoom`이 마스크 모양까지 키웠다(줌 아웃이면 마스크가 화면보다 작아져 가장자리가 비었다).
지금은 마스크가 화면에 고정되고, 그 안의 무대만 카메라가 움직인다. 이 문서에서 유일하게 의도해서 바꾼 화면 동작이다.

### 2.3 빌더가 보장하는 것

`StageRigBuilder.BindRefsFromRoot`는 씬에 남은 무대를 검증한다 — 이름 · 부모 · 자식 집합 · 항등 · 마스크 컴포넌트가 하나라도 어긋나면 자식을 전부 부수고 다시 세운다(경고 "무대 계층이 스키마와 다르다").
파괴 전 `SetParent(null)`은 캐릭터 리그 빌더와 같은 이유다 — 같은 프레임의 이름 검색이 삭제 예정 노드를 다시 잡는다.

그래서 런타임 코드는 무대 노드에 null 검사를 하지 않는다. `StageRigRefs`가 있으면 노드도 있다.

### 2.4 리그는 무대와 층을 옮기지 않는다 (D10)

리그는 slot이 세운 무대 · 층에 산다(`CharacterRigRefs.StandOn` — slot이 한 번 부른다). 다른 자리에 서려면
기존 리그를 `fade_out`으로 치우고 새 무대 · 층에 새 slot을 세운다.

종전의 `char_to`(리그를 다른 무대 · 층으로 옮기는 커맨드)는 2026-11-06에 걷었다. 대본 사용은 0건이었다.

- 무대마다 카메라가 다를 수 있으므로, 옮기는 순간 다른 카메라 아래로 들어가 화면에서 튄다. 같은 무대 안에서도 층이 바뀌면 시차 반응이 달라져 튄다.
- 리그가 무대를 옮기지 않으면 측정 체인의 경계(`Stage.Root`)가 리그가 사는 동안 고정이고, 폴드의 부착(`SlotAttachment`)도 스폰 때 한 번 정해진다.
- 연출로도 의도가 분명하다. "이 캐릭터가 저 화면에 나타난다"는 새 리그 하나다.

### 2.5 tx — 무대를 어디까지 보여 줄지

`<<tx 프리셋 [무대=stage01] [시간]>>`은 무대 하나의 마스크 모양을 움직인다(`StageMaskSlot`). 무대는 조립 순서대로 겹쳐
그려지므로(stage02가 맨 위) 덮는 무대의 마스크를 열고 닫으면 그 모양만큼 본 무대 위에 다른 화면이 보인다. 생략값이
stage01인 이유다. 본 무대의 마스크를 닫으면(`tx daze_close stage00`) 장면 자체가 닫힌다.

| 모양 | 프리셋 |
|---|---|
| 사선 패널 | `slant_in` · `slant_out` |
| 가로 띠 | `hstrip_open/close/in/out` · `shutter_close/open` · `daze_close/open` |
| 세로 띠 | `vstrip_open/close/in/out` |
| 대각 띠 | `band_in` · `band_out` |
| 원 | `iris_in` · `iris_out` |
| 전체 | `strip_cover` · `strip_clear` |

tx는 **보이는 범위만** 맡는다. 샷의 뜻(줌하며 열리기 같은)을 섞지 않고, 연출은 커맨드 순서로 조합한다.
마스크가 카메라 위라(§2.2) 패널 모양은 화면에 고정이고, 덮는 무대는 카메라도 고정이라(§2.1) 패널 안도 흔들리지 않는다.

```
<<slot c1_cut stage01 mid>>                   인서트 리그를 덮는 무대에 새로
<<show c1_cut …>>  <<size …>>  <<place …>>    내용은 열기 전에 확정한다
<<tx slant_in stage01>>                       연다
…
<<tx slant_out stage01>>                      닫는다
<<fade_out c1_cut>>                           치운다
```

- 덮는 무대가 화면 전체를 가리면(`iris_in` · `strip_cover`) 그것은 커튼이다. 가린 뒤에 본 무대를 다시 꾸미고 걷어 낸다. 덮는 무대에서 장면을 이어 가지 않는다.
- 매치 컷(본 무대와 인서트를 같은 구도로 이어 보이기)은 저절로 되지 않는다. 필요하면 인서트 리그를 같은 자리 · 크기로 세운다. 샷을 복사하는 커맨드는 두지 않는다.
- 폴드는 마스크를 모른다(무대 상태 모델 밖, [target-boundary.md](../../../Core/Documentation~/target-boundary.md)).

---

## 3. d — 층의 깊이

| 라벨 | d |
|---|---|
| far | −1 |
| back | −0.5 |
| mid | 0 |
| front | 0.5 |
| close | 1 |

- 표는 코어 `StageLayerLabels` 하나다. 런타임 파서(`StageKeyParser.ParseLayerDepth`)와 폴드(`SlotAttachment`)가 같은 표를 본다.
- 대본은 라벨만 쓴다. 숫자 d는 어휘에 없다(정본화 — 대본은 컴포지터가 낸다). 별칭(`near` · `bg` · `0`~`4` 등)은 걷었다.
- 모르는 라벨은 데이터 오류다 — 런타임은 경고하고 **mid**로 간다. 종전 slot 경로는 far로 갔었다.
- **d는 캐릭터 깊이와 다른 축이다(D4).** 캐릭터 깊이(`size`, level 커브)는 리그 배율이고, 층 d는 카메라에 대한 반응 계수다. 같은 낱말(far · close)을 쓰지만 표가 다르다(`DepthLevelLabels` 대 `StageLayerLabels`).

---

## 4. 샷 식 — `Core/Transforms/ShotLayoutMath.cs`

입력은 샷 의도 `ShotIntentState`(zoom · pan · focus) 하나와 층 d. 층의 기준값은 입력에 없다 — 빌더가 항등을 보장하므로.

### 4.1 카메라

```
cameraScale = 1 + 0.05 × zoom            (ShotIntentMath.EvaluateCameraScale — 0.05는 규약 상수)
Camera_Zoom.localScale       = (cameraScale, cameraScale, 1)
Camera_Pan.anchoredPosition  = pan
```

### 4.2 층 반응

d에서 프로필(`LayerProfile`) 셋을 읽는다: `focusSpreadPerZoom`(축별) · `panResponse`(축별) · `zoomScaleResponse`.

```
offset = sign(0 − focus) × focusSpread × (cameraScale − 1)  +  pan × panResponse
scale  = cameraScale ^ zoomScaleResponse
```

- 첫 항(초점 산개): 줌이 커질수록 층이 초점에서 멀어지는 쪽으로 밀린다. 부호는 축별이고, 초점이 정확히 0인 축은 0이다.
- 둘째 항(팬 추종): 팬을 층마다 다른 비율로 더 따라간다. 가까운 층은 더, 먼 층은 덜 — 시차.
- 배율: 카메라 배율을 층마다 상쇄하거나 강조한다.
- offset은 rig space(무대 루트) 벡터다. 층의 부모는 줌 루트라 배율이 다르므로, 런타임은 벡터로 옮겨 쓴다(`Layer.anchoredPosition = offset / cameraScale`).

### 4.3 층 안의 점이 보이는 자리

```
보이는 자리 = pan + offset + cameraScale × layerScale × p        (ShotLayoutMath.ProjectToRigSpace)
```

p는 층(Content) 안의 점이다. 계층 Pan → Zoom → Layer를 합성한 결과이고, 하네스와 툴 프리뷰가 이 한 줄을 공유한다.

### 4.4 프로필 곡선 — `LayerProfileCurve`

d → 프로필. 표본 사이는 선형, 바깥은 끝 표본으로 고정. 기본 표본(`LayerProfileCurve.Default`)은 종전 다섯 층 값 그대로다(D7).

| d | focusSpreadPerZoom | panResponse | zoomScaleResponse |
|---|---|---|---|
| −1 (far) | (−8, 0) | (−0.04, 0) | −0.15 |
| −0.5 (back) | (−4, 0) | (−0.02, 0) | −0.08 |
| 0 (mid) | 0 | 0 | 0 |
| 0.5 (front) | (4, 0) | (0.02, 0) | 0.04 |
| 1 (close) | (16, 0) | (0.2, 0) | 0.16 |

mid는 항등이다 — mid에 선 리그에게 샷은 카메라 그대로다.

생성자가 지키는 것 둘: 표본은 d 오름차순, **`panResponse`는 축마다 −1보다 크다.**
층이 화면에서 움직이는 양은 `pan × (1 + panResponse)`라, −1이면 그 층은 팬을 전혀 따라가지 않는다(무한히 먼 층).
그런 층의 점은 팬으로 화면 지점에 놓을 수 없다(§7). 표본이 모두 −1보다 크면 보간한 값도 그렇다.

### 4.5 곡선의 진실은 런타임에 있다

런타임 조립은 `LayerProfileCurve.Default`를 쓴다. 폴드와 하네스는 `presets/layer-profile.json`이 있으면 그것을, 없으면 같은 기본값을 쓴다(`StageReducerTuning.ResolveLayerProfileCurve`).
json은 다른 프리셋처럼 **런타임 값의 덤프**다(튜닝 덤프 메뉴 → `PresentationTuningExporter.ExportLayerProfile`). 손으로 고치면 런타임과 폴드가 갈린다 — 곡선을 바꾸려면 코드의 `Default`를 바꾸고 다시 덤프한다.

---

## 5. 중립 = `Solve(Default)` (D6)

`place` · `size` · `shot_focus_to`는 무대 좌표로 잰다 — "카메라가 없을 때 리그가 무대의 어디에 있나"(`a9bc19f7`).
측정 체인(`CharacterPlacementTargetLedger.CaptureSettledChain`)이 샷 소유 노드를 지날 때는 라이브 대신 중립값을 쓴다.

중립은 따로 적은 정의가 없다. 같은 식에 `ShotIntentState.Default`(zoom 0 · pan 0 · focus 0)를 넣은 값이다:
cameraScale 1, pan 0, 층 offset 0, 층 배율 1 — 즉 항등.

어느 노드가 샷 소유인지는 빌더 출력이 안다: `StageRigRefs.IsShotOwned(rect)` — 카메라 둘이거나 어느 층의 Rect.
`PresentationShotResponseSystem.TryGetNeutral`은 이 목록에서 찾아 `Solve(Default)`를 돌려준다. 종전의 참조 비교 분기와 "바인드 때 잰 기준값"은 없다.

체인 전체에서 이 단계가 어디에 서는지는 [settled-chain.md](../../ShotResponse/Documentation~/settled-chain.md).

---

## 6. 흐름 — 누가 무엇을 하나

```
조립 (SampleBootstrap.Presentation)
  StageRigRegistry.Build(IStageMount, "stage00"…, 층 다섯)     무대 셋을 세운다
  new PresentationShotResponseSystem(registry, LayerProfileCurve.Default)
  PresentationStage.AttachStageView(샷 시스템)                    측정이 중립을 묻는 곳

slot / bg_spawn
  StageKeyParser          무대 키(문자열, 생략하면 본 무대) · 라벨 → d
  CharRigSlotResolver     registry.Get(무대) + EnsureLayer(d)
  리그 루트 → Layer.Content,  CharacterRigRefs.StandOn(무대, 층) — 리그가 사는 동안 그대로

shot_zoom / shot_track / shot_to / shot_reset / shot_focus_to
  ShotIntentCommandBase   BuildTargetState → 새 intent
  PresentationShotResponseSystem.Follow(intent)
    → 트윈 하나가 intent를 보간(D9) → Apply
    → 본 무대에만: SolveCamera → 카메라 둘,  SolveLayer(d) → 층마다 위치 · 배율
    → 덮는 무대: 쓰지 않는다(빌더가 세운 항등 = 중립)

tx
  StageMaskSlot           그 무대의 마스크 모양만. 카메라 · 리그와 무관

place / size / shot_focus_to 측정
  CaptureSettledChain(측정 노드 → rigRefs.Stage.Root)
    노드마다: 장부(리그 끝값) → 샷 중립(Solve(Default)) → 라이브

블러 (screen_blur)
  RigBlurIntentSystem     (무대 키, d)로 층의 Content를 고른다

정지 프레임 (StageReducer — 코어)
  slot · bg_spawn         SlotAttachment(무대 키, 라벨 → LayerDepth) — 스폰 때 한 번
  shot_*                  StageState.Shot 하나를 갈아끼운다
  층 반응은 저장하지 않는다 — Shot + d로 언제든 푼다

등가 하네스 (StageEquivalenceHarness)
  캡처한 무대 뷰(카메라 · 층) == 본 무대 Solve(접힌 Shot), 덮는 무대 Solve(Default)
```

`RigSpaceRoot`는 무대마다 그 무대의 `Stage_Root`다. 세 무대 루트는 같은 자리(뷰포트를 꽉 채운 항등)라 무대 좌표는 무대가 달라도 같은 좌표계다.
샷이 본 무대만 움직이므로, 잰 점이 화면 어디에 보이는지가 샷에 따라 달라지는 것은 본 무대의 리그뿐이다.

---

## 7. `shot_focus_to` — 층 반응까지 넣어 팬을 푼다

`shot_focus_to c1 bust center 2.5` — 캐릭터의 초점이 화면의 지점에 오는 zoom · pan.

### 7.1 무엇이 틀렸었나

종전에는 런타임과 코어 모두 팬을 카메라만 보고 풀었다: `pan = desired − p × cameraScale`.
mid에서는 맞지만, 다른 층에 선 리그는 층 반응만큼 빗나갔다. place_left bust(초점 ≈ (−461, 300)), center 2.5 기준:

| 층 | 빗나감 (px) |
|---|---|
| far | (−12.7, −5.9) |
| front | (8.4, 1.6) |
| close | (95.8, 6.4) |

런타임과 코어가 똑같이 틀려서 등가 하네스는 잡지 못했다 — 하네스는 둘이 같은지를 보지, 맞는지를 보지 않는다.

### 7.2 식

zoom과 focus가 정해지면(zoom은 인자, focus는 그 캐릭터의 점 p), 보이는 자리(§4.3)에서 팬에 기대는 항은
카메라가 미는 `pan`과 층이 더 미는 `pan × panResponse` 둘뿐이다. 나머지(산개 · 배율)는 상수가 된다. 그래서

```
보이는 자리 = pan × (1 + panResponse) + [팬 0일 때 보이는 자리]
```

팬 1px당 그 점이 화면에서 움직이는 양이 층마다 일정하다(mid 1, close 1.2, far 0.96). 거꾸로 풀면 나눗셈 한 번이다:

```
pan = (desired − [팬 0일 때 보이는 자리]) / (1 + panResponse)        축마다
```

x 팬은 x만, y 팬은 y만 움직이므로 두 축을 따로 푼다. `[팬 0일 때 보이는 자리]`는 정방향 식(§4.2 · §4.3)을 팬 0으로 불러 얻는다 — 산개 · 배율 식을 다시 적지 않는다.
mid에서는 종전 식과 같다.

close 가로축 예시:

| | 팬 0일 때 보이는 자리 | 팬 1px당 | 팬 | 결과 |
|---|---|---|---|---|
| 종전 | −518.4로 가정 | 1로 가정 | 518.4 | +95.9에 찍힘 |
| 지금 | −526.2 (산개 · 층 배율 포함) | 1.2 | 438.5 | 0 |

### 7.3 어디에 있나

| 자리 | 하는 일 |
|---|---|
| `ShotLayoutMath.SolvePanToPlace` | 위의 역산 |
| `ShotIntents.FocusTo(zoom, focus, desired, d, 곡선)` | 팬을 풀고 intent(zoom, pan, focus)를 만든다. 논리 focus = 그 점 |
| 런타임 `ShotZoomFocusCommand` | 측정(무대 좌표 · 무대 · 층) → 덮는 무대면 경고하고 건너뜀 → `Layer.Depth` · `rig.LayerProfile`로 목표 계산 → Follow |
| 코어 `StageReducer.ApplyShotFocusTo` | 덮는 무대면 Unhandled → 폴드의 focus → `GetAttachment(slot).LayerDepth` · `ResolveLayerProfileCurve()`로 같은 목표 계산 |

입력이 이미 무대 좌표라 "지금 카메라를 씌웠다 벗기는" 왕복이 없다. 결과는 무대 위 자리와 층만으로 정해진다 — 지금 카메라 · 샷의 진행도와 무관하다.

---

## 8. 폴드와 하네스

### 8.1 층 반응은 상태에 저장하지 않는다

`StageState`에는 샷 의도(`Shot`)와 슬롯 부착(`SlotAttachment` — 무대 키 · 라벨 · `LayerDepth`)만 있다.
층 반응은 그 둘에서 `ShotLayoutMath`로 언제든 풀리는 값이라, 저장하면 같은 값이 두 곳에 산다.
정지 프레임을 그리거나 판정하는 쪽이 푼다.

### 8.2 하네스가 보는 것

`StageStateCapture.CaptureStageView`가 무대마다 카메라(팬 · 줌 배율)와 층마다 **무대 루트에서 본 중심 · 전체 배율**을 캡처한다(`StageViewSnapshot`).
런타임이 값을 카메라와 층에 어떻게 나눠 썼는지는 보지 않는다 — Unity가 합성한 보이는 결과만 읽는다.

`StageStateComparer.CompareStageView`의 기대값은 접힌 샷 의도를 코어 식으로 푼 값이다:

```
층 중심     = ProjectToRigSpace(0, cameraScale, pan, SolveLayer(Shot, d))
층 전체 배율 = SolveLayer(Shot, d).Scale × cameraScale
```

Shot은 본 무대면 접힌 샷, 덮는 무대면 `Default`(항등)다 — 덮는 무대가 샷을 따랐으면 불일치로 잡힌다.

불일치 키는 `stage:{무대}` (cameraScale · cameraPan), `stage:{무대}/layer:{d}` (center · totalScale). 리포트에 "층 N개 비교"가 찍힌다.

---

## 9. 결정

| # | 결정 |
|---|---|
| D1 | 무대 계층은 `StageRigSchema` + `StageRigBuilder`가 세운다. 씬은 빌더가 검증 · 재건하는 대상이다 |
| D2 | 카메라는 무대 루트 아래. 무대마다 카메라가 있고, 어느 무대를 샷이 움직이는지는 정책이다 — 2026-11-06부터 본 무대만(D11) |
| D3 | 층은 d 하나로 식별한다. 위치 · 배율을 한 노드에 쓴다. 라벨은 어휘에 남고 d의 눈금이다 |
| D4 | d는 캐릭터 깊이와 별도의 축이다 |
| D5 | 샷 식은 코어 `ShotLayoutMath` 하나. 기준값 입력 없음 |
| D6 | 중립 = `Solve(Default)`. 별도의 정의를 두지 않는다 |
| D7 | 프로필 숫자 · 산개 부호는 종전 그대로. 바뀐 동작은 마스크 위치(§2.2) · `shot_focus_to` 명중(§7) · 덮는 무대 카메라 고정(§2.1) — 모두 결정해서 바꿨다. 덮는 무대는 대본 사용이 0건이라 화면은 그대로다 |
| D8 | 카메라 오브젝트는 도입하지 않는다. "카메라 없음 = 항등" |
| D9 | 전환은 intent를 보간하고 매 프레임 다시 푼다. 트윈 하나 |
| D10 | 리그는 무대 · 층을 옮기지 않는다. char_to 제거 — 다른 자리는 fade_out 뒤 새 slot (2026-11-06) |
| D11 | 무대의 역할: 본 무대(stage00)만 샷을 따르고, 덮는 무대(stage01 · stage02)는 중립에 고정. 같은 세계는 층으로, 다른 화면은 무대로 (2026-11-06) |

## 10. 커밋

| 커밋 | 단계 | 내용 |
|---|---|---|
| `3ad48c54` | P1 | 샷 식을 코어로 — `ShotLayoutMath` · `LayerProfileCurve` · `StageLayerLabels`. UnityParity 오라클(런타임 == 코어) |
| `8d1a4eef` | P2 · P3 | 스키마 · 빌더 · 레지스트리. 샷 시스템이 코어 식의 applier가 됨. 씬 `StageShot_Root` 서브트리 · 인터페이스 셋 · enum 둘 · 별칭 표 둘 삭제 |
| `229d15ef` | P4 | 폴드의 부착 d, 하네스가 카메라 · 층까지 판정 |
| `a7974fa2` | P4.5 | `shot_focus_to`가 층 반응까지 넣어 명중 |
| `8523b0e3` | 후속 | char_to 제거 — 리그는 무대 · 층을 옮기지 않는다 |
| (11-06) | 후속 | 무대의 역할 — 샷은 본 무대만 |

## 11. 범위 밖 · 남은 것

- 덮는 무대의 카메라 연출(`shot_*`의 무대 인자, `StageState.Shot`을 무대별로). 구조는 열려 있고 생략값은 본 무대다(§2.1).
- 컴포지터(`ked-story-compositor`) 코어 사본 동기화 — 그 전까지 컴포지터 폴드는 층 반응도, 층을 넣은 `shot_focus_to`도 모른다.
- `ScreenFocusPoint` → `StagePoint` / `ScreenPoint` 분리.
- 프로필 곡선의 모양 · 숫자 조정(미감).

---

관련 문서: [settled-chain.md](../../ShotResponse/Documentation~/settled-chain.md) (장부 · 샷 중립 · 라이브 — 측정 체인),
[target-boundary.md](../../../Core/Documentation~/target-boundary.md) (목표 계산 규약),
[rig-axis-order.md](../../CharacterRig/Documentation~/rig-axis-order.md) (캐릭터 리그가 스키마로 간 이유)
