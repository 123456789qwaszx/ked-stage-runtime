using Ked.Presentation.Core;
using UnityEngine;

public sealed partial class StageCommandParser
{
    // <<tx 프리셋 [무대=stage01] [시간]>> — 프리셋은 StageMaskMotionPresetDB의 키
    // (slant_in/out · hstrip_open/close/in/out · vstrip_open/close/in/out · band_in/out · iris_in/out ·
    // shutter_close/open · daze_close/open · strip_cover/clear). 옛 tx_<이름> 18개는 이 한 줄로 접혔다(2026-11-03).
    // 시간이 비면 override 없음 — 프리셋 자체 duration을 쓴다.
    private void EnqueueStageMaskMotionPresetSpec(
        string presetKey,
        string stage,
        string durationToken)
    {
        if (string.IsNullOrWhiteSpace(presetKey))
        {
            Debug.LogWarning("[StageCommandParser] <<tx>>에 프리셋이 없다. 건너뛴다.");
            return;
        }

        Emit(new StageMaskMotionCommandSpec
        {
            stage = PresentationStageKeyParser.Parse(stage),
            presetKey = StageMaskMotionPresetDBSO.NormalizeKey(presetKey),
            durationOverride = string.IsNullOrEmpty(durationToken)
                ? -1f
                : YarnDurationParser.Parse(durationToken)
        });
    }

    private void EnqueueStageMaskClearSpec()
    {
        Emit(new StageMaskClearCommandSpec
        {
            stage = PresentationStageKey.Stage00,
            mode = StageMaskClearMode.UnmaskedFullVisible,
            hideEdge = true
        });
        Emit(new StageMaskClearCommandSpec
        {
            stage = PresentationStageKey.Stage01,
            mode = StageMaskClearMode.UnmaskedFullVisible,
            hideEdge = true
        });
        Emit(new StageMaskClearCommandSpec
        {
            stage = PresentationStageKey.Stage02,
            mode = StageMaskClearMode.UnmaskedFullVisible,
            hideEdge = true
        });
    }
}