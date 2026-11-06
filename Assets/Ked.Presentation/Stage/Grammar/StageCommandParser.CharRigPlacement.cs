using Ked.Presentation.Core;
using DG.Tweening;
using UnityEngine;

public sealed partial class StageCommandParser
{
    private const string DefaultShowFaceToken = "e1";
    private const string DefaultShowDurationToken = "14fr";
    
    private const string DefaultNudgeDurationToken = "8fr";
    
    #region Show
    private void EnqueueShowAtSpec(string roleKey, string faceToken, string durationToken)
    {
        var spec0SetAnchor = new SetAnchorCommandSpecCharR
        {
            roleKey = roleKey,
            resetSlotPos = true,
            resetCharacterPos = true
        };
        
        var spec1SetPortraitSprite = new SetPortraitSpriteCommandSpecCharR
        {
            roleKey = roleKey,
            portrait = new PortraitIdentity { emotion = ShowFaceAliasParser.Parse(faceToken) }
        };
        
        var spec2FadeInCharRigRoot = new FadeCommandSpecCharR
        {
            node = CharacterRigNode.RigRoot,
            targetAlpha = NodeTargets.FadeInAlpha,
            
            roleKey = roleKey,
            duration = YarnDurationParser.Parse(durationToken)
        };
        
        var spec3FadeInPortraitSprite = new FadeCommandSpecCharR
        {
            node = CharacterRigNode.CharacterPortraitSprite_Root,
            targetAlpha = NodeTargets.FadeInAlpha,
            
            roleKey = roleKey,
            duration = YarnDurationParser.Parse(durationToken)
        };

        Emit(spec0SetAnchor);
        Emit(spec1SetPortraitSprite);
        Emit(spec2FadeInCharRigRoot);
        Emit(spec3FadeInPortraitSprite);
    }
    #endregion
    
    #region Nudge
    private void EnqueueDirectionalNudgeSpec(
        string roleKey,
        float xSign,
        float ySign,
        string unitToken,
        string durationToken,
        string easeToken,
        CharacterRigNode node)
    {
        float pixels = YarnUnitParser.Parse(unitToken);
        float duration = YarnDurationParser.Parse(durationToken);

        // 미지정이면 종전 하드코딩 값과 같은 OutCubic이다 — 기존 대본 불변.
        Ease ease = YarnEaseParser.Parse(easeToken);

        Emit(new MoveByCommandSpecCharR
        {
            roleKey = roleKey,
            node = node,
            useAbsolutePosition = false,
            delta = new Vector2(pixels * xSign, pixels * ySign),
            duration = duration,
            ease = ease
        });
    }
    
    
    private void EnqueueNudgeLeftSpec(string roleKey, string unitToken, string durationToken, string easeToken)
        => EnqueueDirectionalNudgeSpec(roleKey, -1f, 0f, unitToken, durationToken, easeToken, CharacterRigNode.CharSlot_Track_X);

    
    private void EnqueueNudgeRightSpec(string roleKey, string unitToken, string durationToken, string easeToken)
        => EnqueueDirectionalNudgeSpec(roleKey, 1f, 0f, unitToken, durationToken, easeToken, CharacterRigNode.CharSlot_Track_X);

    
    private void EnqueueNudgeUpSpec(string roleKey, string unitToken, string durationToken, string easeToken)
        => EnqueueDirectionalNudgeSpec(roleKey, 0f, 1f, unitToken, durationToken, easeToken, CharacterRigNode.CharSlot_Track_Y);

    
    private void EnqueueNudgeDownSpec(string roleKey, string unitToken, string durationToken, string easeToken)
        => EnqueueDirectionalNudgeSpec(roleKey, 0f, -1f, unitToken, durationToken, easeToken, CharacterRigNode.CharSlot_Track_Y);
    #endregion
}
