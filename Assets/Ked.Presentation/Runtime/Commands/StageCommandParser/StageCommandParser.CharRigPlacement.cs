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
            slotKey = roleKey,
            resetSlotPos = true,
            resetCharacterPos = true
        };
        
        var spec1SetPortraitSprite = new SetPortraitSpriteCommandSpecCharR
        {
            slotKey = roleKey,
            portrait = new PortraitIdentity { emotion = ShowFaceAliasParser.Parse(faceToken) }
        };
        
        var spec2FadeInCharRigRoot = new FadeInCommandSpecCharR
        {
            target = CharacterRigTarget.RigRoot,
            
            slotKey = roleKey,
            duration = YarnDurationParser.Parse(durationToken)
        };
        
        var spec3FadeInPortraitSprite = new FadeInCommandSpecCharR
        {
            target = CharacterRigTarget.CharacterPortraitSprite_Root,
            
            slotKey = roleKey,
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
        CharacterRigTarget target)
    {
        float pixels = YarnUnitParser.Parse(unitToken);
        float duration = YarnDurationParser.Parse(durationToken);

        // 미지정이면 종전 하드코딩 값과 같은 OutCubic이다 — 기존 대본 불변.
        EaseSelection ease = ResolveEase(easeToken);

        Emit(new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = target,
            useAbsolutePosition = false,
            delta = new Vector2(pixels * xSign, pixels * ySign),
            duration = duration,
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys
        });
    }
    
    
    private void EnqueueNudgeLeftSpec(string roleKey, string unitToken, string durationToken, string easeToken)
        => EnqueueDirectionalNudgeSpec(roleKey, -1f, 0f, unitToken, durationToken, easeToken, CharacterRigTarget.CharSlot_Track_X);

    
    private void EnqueueNudgeRightSpec(string roleKey, string unitToken, string durationToken, string easeToken)
        => EnqueueDirectionalNudgeSpec(roleKey, 1f, 0f, unitToken, durationToken, easeToken, CharacterRigTarget.CharSlot_Track_X);

    
    private void EnqueueNudgeUpSpec(string roleKey, string unitToken, string durationToken, string easeToken)
        => EnqueueDirectionalNudgeSpec(roleKey, 0f, 1f, unitToken, durationToken, easeToken, CharacterRigTarget.CharSlot_Track_Y);

    
    private void EnqueueNudgeDownSpec(string roleKey, string unitToken, string durationToken, string easeToken)
        => EnqueueDirectionalNudgeSpec(roleKey, 0f, -1f, unitToken, durationToken, easeToken, CharacterRigTarget.CharSlot_Track_Y);
    #endregion
}
