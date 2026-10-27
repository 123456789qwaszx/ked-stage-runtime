public sealed class StageDepthLayerBinder
{
    private readonly IShotResponseStageProvider _provider;

    private bool _bound;

    private readonly PresentationResponseProfile _far = PresentationResponseProfile.DepthFar;
    private readonly PresentationResponseProfile _back = PresentationResponseProfile.DepthBack;
    private readonly PresentationResponseProfile _mid = PresentationResponseProfile.DepthMid;
    private readonly PresentationResponseProfile _front = PresentationResponseProfile.DepthFront;
    private readonly PresentationResponseProfile _close = PresentationResponseProfile.DepthClose;

    public StageDepthLayerBinder(IShotResponseStageProvider provider)
    {
        _provider = provider;
    }

    // 생성자에서 바인딩하지 않는 이유:
    // depth layer rect들이 stretch 앵커로 부모 크기에 의존하므로,
    // Canvas가 첫 레이아웃을 돌리기 전(부트스트랩 Awake)에 캡처하면
    // baseMeasure의 basePositionInRigSpace가 틀어질 수도 있음.
    public void EnsureBindings(PresentationShotResponseSystem rig)
    {
        if (_bound)
            return;

        BindStage(rig, PresentationStageKey.Stage00);
        BindStage(rig, PresentationStageKey.Stage01);
        BindStage(rig, PresentationStageKey.Stage02);

        _bound = true;
    }

    private void BindStage(
        PresentationShotResponseSystem rig,
        PresentationStageKey root)
    {
        Bind(rig, root, PresentationDepthLayerKey.Far, _far);
        Bind(rig, root, PresentationDepthLayerKey.Back, _back);
        Bind(rig, root, PresentationDepthLayerKey.Mid, _mid);
        Bind(rig, root, PresentationDepthLayerKey.Front, _front);
        Bind(rig, root, PresentationDepthLayerKey.Close, _close);
    }

    private void Bind(
        PresentationShotResponseSystem rig,
        PresentationStageKey root,
        PresentationDepthLayerKey layer,
        PresentationResponseProfile profile)
    {
        string key = $"{root}/{layer}";

        StageDepthLayerRects rects = _provider.GetLayerRects(root, layer);

        var target = new StageDepthResponseTarget(
            rects.Measure,
            rects.Position,
            rects.Scale);

        rig.RegisterRuntimeBinding(
            key,
            target,
            profile);
    }
}