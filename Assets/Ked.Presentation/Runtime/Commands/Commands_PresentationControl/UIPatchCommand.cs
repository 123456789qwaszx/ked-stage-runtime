using System;
using System.Collections;
using UnityEngine;

[Serializable]
public sealed class UIPatchCommandSpec : CommandSpecBase
{
    [Header("Context")]
    public string themeId = "default";
    public string localeId = "ko-KR";
}

public sealed class UIPatchCommand : CommandBase
{
    private readonly IUIThemePatchPort _uiThemePatch;
    private readonly UIPatchCommandSpec _spec;

    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;
    
    public UIPatchCommand(IUIThemePatchPort uiThemePatch, UIPatchCommandSpec spec)
    {
        _uiThemePatch = uiThemePatch;
        _spec = spec;
    }
    
    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        yield return _uiThemePatch.PatchCurrentScreen(_spec.themeId, _spec.localeId);
    }
}