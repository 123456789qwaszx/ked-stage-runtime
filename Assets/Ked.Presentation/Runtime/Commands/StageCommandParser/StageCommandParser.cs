using System.Collections.Generic;
using Ked.Presentation.Core;
using UnityEngine;

/// <summary>
/// StageCommand(이름 + 문자열 인자) → CommandSpec. 대본 문법의 뜻을 정하는 한 곳이다.
///
/// Yarn 브리지는 텍스트에서 StageCommand를 뽑아 여기 넘기기만 하고, 등가성 하네스의
/// 폴드(StageReducer)도 같은 StageCommand를 받는다 — 같은 줄을 읽는 파서가 둘이면
/// "툴에서는 되는데 재생에서 사라지는" 커맨드가 생기므로 뜻은 여기 한 곳에만 둔다.
///
/// 인자 자리와 생략값은 Table(StageCommandParser.Table.cs)에 있다. 핸들러 본문은
/// 옮기기 전 YarnCommandBridge의 것 그대로다 — 값·순서·부작용 변경 없음.
/// </summary>
public sealed partial class StageCommandParser
{
    private readonly RectTransform _charRigPrefab;
    private readonly RectTransform _backgroundRigPrefab;
    private readonly EaseCurveLibrary _easeCurves;

    private List<CommandSpecBase> _output;

    public StageCommandParser(
        RectTransform charRigPrefab,
        RectTransform backgroundRigPrefab,
        EaseCurveLibrary easeCurves = null)
    {
        _charRigPrefab = charRigPrefab;
        _backgroundRigPrefab = backgroundRigPrefab;
        _easeCurves = easeCurves ?? EaseCurveLibrary.Empty;
    }

    public static bool Knows(string commandName)
        => commandName != null && Table.ContainsKey(commandName);

    /// <summary>
    /// 커맨드 하나를 스펙(0개 이상)으로 풀어 output 뒤에 붙인다.
    /// 모르는 이름이면 false — 호출부가 소리 낸다(침묵 금지).
    /// </summary>
    public bool TryParse(StageCommand command, List<CommandSpecBase> output)
    {
        if (!Table.TryGetValue(command.Name, out Handler handler))
            return false;

        _output = output;

        try
        {
            handler(this, command);
        }
        finally
        {
            _output = null;
        }

        return true;
    }

    private void Emit(CommandSpecBase spec) => _output.Add(spec);

    // 숫자 인자. 비면 fallback(생략), 못 읽으면 경고 + fallback — 조용히 0이 되지 않는다.
    private static float Float(StageCommand c, int index, float fallback)
    {
        string token = c.Arg(index);

        if (token == null)
            return fallback;

        if (YarnNumberParser.TryParseFloat(token, out float value))
            return value;

        Debug.LogWarning(
            $"[StageCommandParser] <<{c.Name}>> 인자 {index}가 숫자가 아니다 ('{token}'). {fallback}으로 간다.");

        return fallback;
    }
}
