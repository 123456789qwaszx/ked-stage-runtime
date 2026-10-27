using Ked.Presentation.Core;
public sealed partial class StageCommandParser
{
    // focus_on @1            — @1에 초점, 나머지는 깊이 차이만큼 흐려진다
    // focus_on @1 0.6        — 최대 흐림을 0.6으로 제한
    // focus_on @1 0.6 0.8s   — 0.8초에 걸쳐
    private void EnqueueDepthFocusOnSpec(string slotKey, float maxBlur, string durationToken) 
        => Emit(new DepthFocusCommandSpecCharR
        {
            slotKey = slotKey,
            maxBlur = maxBlur,
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueDepthFocusClearSpec(string durationToken)
        => Emit(new DepthFocusCommandSpecCharR
        {
            clear = true,
            duration = YarnDurationParser.Parse(durationToken)
        });
}