using UnityEngine;

/// <summary>리그가 설 자리 — 무대의 깊이 층 Content. 층은 없으면 세운다. 리그는 그 무대와 층을 기억한다(CharacterRigRefs.Stage · Layer).</summary>
public sealed class CharRigSlotResolver
{
    private readonly StageRigRegistry _stages;

    public CharRigSlotResolver(StageRigRegistry stages)
    {
        _stages = stages;
    }

    public StageLayerRefs Resolve(string stageKey, float layerDepth, out StageRigRefs stage)
    {
        stage = _stages.Get(stageKey);
        return _stages.EnsureLayer(stageKey, layerDepth);
    }
}
