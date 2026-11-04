public interface IStageMaskProvider
{
    StageMaskSlot GetStageMaskSlot(PresentationStageKey stage);

    bool TryGetStageMaskSlot(
        PresentationStageKey stage,
        out StageMaskSlot slot);

    bool TryGetStageMaskGraphic(
        PresentationStageKey stage,
        out StageMaskGraphic graphic);
}

public static class StageMaskProviderExtensions
{
    // 장면 경계 — 무대마다 tx 모션을 끊고 씬에 저장된 기준 모양으로.
    public static void ResetAllToBaseline(this IStageMaskProvider provider)
    {
        if (provider == null)
            return;

        for (int i = 0; i < (int)PresentationStageKey.Count; i++)
        {
            if (provider.TryGetStageMaskSlot((PresentationStageKey)i, out StageMaskSlot slot) && slot != null)
                slot.ResetToBaseline();
        }
    }
}
