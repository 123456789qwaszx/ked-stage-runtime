using Ked.Presentation.Core;
using DG.Tweening;
using UnityEngine;

public sealed partial class StageCommandParser
{
    private const string DefaultBackgroundSpriteLayerToken = "back";

    private void EnqueueSpawnBackgroundRigSpec(string rigKey, string spriteKey)
    {
        EnqueueSetupBackgroundRigSpec(rigKey);
        EnqueueSetBackgroundSpriteSpec(rigKey, spriteKey, DefaultBackgroundSpriteLayerToken);
    }

    private void EnqueueSetupBackgroundRigSpec(string rigKey)
        => Emit(new SetupBackgroundRigCommandSpec
        {
            rigKey = rigKey,
            rigPrefab = _backgroundRigPrefab,
            
            stage = PresentationStageKey.Stage00,
            layer = PresentationDepthLayerKey.Far,
        });
    private void EnqueueSpawnBackgroundRigStage00Spec(string rigKey, string spriteKey, string layerKey)
        => EnqueueSpawnBackgroundRigAtDepthSpec(rigKey, spriteKey, PresentationStageKey.Stage00, layerKey);

    private void EnqueueSpawnBackgroundRigStage01Spec(string rigKey, string spriteKey, string layerKey)
        => EnqueueSpawnBackgroundRigAtDepthSpec(rigKey, spriteKey, PresentationStageKey.Stage01, layerKey);

    private void EnqueueSpawnBackgroundRigStage02Spec(string rigKey, string spriteKey, string layerKey)
        => EnqueueSpawnBackgroundRigAtDepthSpec(rigKey, spriteKey, PresentationStageKey.Stage02, layerKey);

    private void EnqueueSpawnBackgroundRigAtDepthSpec(
        string rigKey,
        string spriteKey,
        PresentationStageKey stage,
        string layerKey)
    {
        EnqueueSetupBackgroundRigAtDepthSpec(
            rigKey,
            stage,
            PresentationDepthLayerKeyParser.Parse(layerKey));

        EnqueueSetBackgroundSpriteSpec(rigKey, spriteKey, DefaultBackgroundSpriteLayerToken);
    }

    private void EnqueueSetupBackgroundRigAtDepthSpec(
        string rigKey,
        PresentationStageKey stage,
        PresentationDepthLayerKey layer)
        => Emit(new SetupBackgroundRigCommandSpec
        {
            rigKey = rigKey,
            rigPrefab = _backgroundRigPrefab,

            stage = stage,
            layer = layer
        });

    private void EnqueueSetBackgroundSpriteSpec(string rigKey, string spriteKey, string layerKey)
        => Emit(new SetBackgroundSpriteCommandSpecBgR
        {
            rigKey = rigKey,
            spriteKey = spriteKey,
        });

    private void EnqueueSetBackgroundOriginSizeSpec(string rigKey, string scaleArg)
    {
        if (!YarnNumberParser.TryParseFloat(scaleArg, out float absoluteScale))
            absoluteScale = 1f;

        var spec = new SetOriginSizeCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Size,

            overrideScale = true,
            scaleOverride = new Vector3(absoluteScale, absoluteScale, absoluteScale)
        };

        Emit(spec);
    }

    private void EnqueueSetBackgroundAnchorDslSpec(string rigKey, string xToken, string yToken, float rotationZ)
        => Emit(new SetAnchorCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Anchor,
            anchoredPosition = new Vector2(ParseSignedUnit(xToken), ParseSignedUnit(yToken)),
            rotationZ = rotationZ
        });

    private void EnqueueFadeInBackgroundDslSpec(string rigKey, string durationToken)
        => Emit(new FadeInCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Root,
            duration = YarnDurationParser.Parse(durationToken, 0.4f),
        });

    private void EnqueueFadeOutBackgroundDslSpec(string rigKey, string durationToken)
        => Emit(new FadeOutCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Root,
            duration = YarnDurationParser.Parse(durationToken, 0.4f),
        });
    
    private void EnqueueMoveBackgroundDslSpec(string rigKey, string xToken, string yToken, string durationToken)
        => Emit(new MoveByCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track_Move,
            delta = new Vector2(ParseSignedUnit(xToken), ParseSignedUnit(yToken)),
            duration = YarnDurationParser.Parse(durationToken, 0.4f),
            ease = Ease.OutCubic
        });

    private void EnqueueScaleBackgroundDslSpec(string rigKey, float scale, string durationToken)
        => Emit(new ScaleToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Scale,
            toScale = new Vector2(scale, scale),
            duration = YarnDurationParser.Parse(durationToken, 0.4f)
        });

    private static float ParseSignedUnit(
        string token,
        float fallbackUnits = 0f)
    {
        if (string.IsNullOrWhiteSpace(token))
            return YarnUnitParser.Parse(token, fallbackUnits);

        string trimmed = token.Trim();

        if (trimmed.StartsWith("-", System.StringComparison.Ordinal))
            return -YarnUnitParser.Parse(trimmed[1..], Mathf.Abs(fallbackUnits));

        if (trimmed.StartsWith("+", System.StringComparison.Ordinal))
            return YarnUnitParser.Parse(trimmed[1..], Mathf.Abs(fallbackUnits));

        return YarnUnitParser.Parse(trimmed, fallbackUnits);
    }
}