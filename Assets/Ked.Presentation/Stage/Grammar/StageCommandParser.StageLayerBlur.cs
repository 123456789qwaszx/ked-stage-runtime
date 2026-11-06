using Ked.Presentation.Core;
using UnityEngine;

public sealed partial class StageCommandParser
{
    // <<screen_blur [층=mid] [양=1] [무대=stage00]>>
    private void EnqueueLayerBlurSpec(
        string stageKey,
        string layerKey,
        float amount)
    {
        Emit(new StageLayerBlurCommandSpec
        {
            stage = StageKeyParser.ParseStage(stageKey),
            layerDepth = StageKeyParser.ParseLayerDepth(layerKey),

            // 옛 blurRadius는 0~8 범위였다. 새 amount는 0~1이라 조정 필요함.
            amount = Mathf.Clamp01(amount)
        });
    }

    // 무대 · 층 전부 — 집합은 조립이 정하므로 파서가 열거하지 않는다.
    private void EnqueueStageLayerBlurClearSpec()
    {
        Emit(new StageLayerBlurCommandSpec
        {
            clearAll = true,
            amount = 0f
        });
    }
}
