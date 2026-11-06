using System;
using System.Collections.Generic;
using UnityEngine;

// alias->alias 체인은 단일 단계만 치환. (actor @ @2)
// 다중 alias 사용 시, 노드 시작 부에서 필요한 것들 전부 직접 다시 선언할 것.
public sealed class CharacterRoleAliasRegistry
{
    // @Actor 와 @actor을 다르게 봄.
    private readonly Dictionary<string, string> _aliases = new(StringComparer.Ordinal);
    
    public void Register(string aliasSymbol, string roleKey)
    {
        if (!IsAliasShaped(aliasSymbol))
        {
            Debug.LogWarning(
                $"[CharacterRoleAliasRegistry] Invalid alias symbol '{aliasSymbol}'. Alias must start with '@'.");

            return;
        }

        _aliases[aliasSymbol.Trim()] = roleKey.Trim();
    }
    
    
    // raw가 등록된 alias면 target key로 치환, 아니면 그대로 반환.
    public string Resolve(string raw)
    {
        if (_aliases.TryGetValue(raw, out string target) && !string.IsNullOrEmpty(target))
            return target;

        // alias 모양(@로 시작)인데 등록 안 된 경우만 경고. 일반 키는 조용히 통과.
        if (IsAliasShaped(raw))
        {
            Debug.LogWarning(
                $"[CharacterRoleAliasRegistry] Alias '{raw}' was used, but not registered. " +
                $"Call <<actor {raw} \u003croleKey\u003e>> first.");
        }

        return raw;
    }

    public bool TryPeek(string aliasSymbol, out string roleKey)
        => _aliases.TryGetValue(aliasSymbol, out roleKey);

    public void Clear()
    {
        _aliases.Clear();
    }

    private static bool IsAliasShaped(string raw)
    {
        return !string.IsNullOrEmpty(raw) && raw[0] == '@';
    }
}