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