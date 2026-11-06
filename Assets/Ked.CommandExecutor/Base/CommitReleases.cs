using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 확정할 때 함께 놓을 것들 — 커맨드가 잡아 둔 예약을 확정과 같은 순간에 푼다.
///
/// 예약을 거는 쪽(무대의 정착 장부 등)이 걸면서 여기에 스스로 해제를 등록한다.
/// 그래서 커맨드가 해제를 따로 챙길 일이 없고, 잊을 수도 없다.
/// 실행기는 무엇을 푸는지 모른다 — 무대를 모른다.
/// </summary>
public sealed class CommitReleases
{
    private readonly List<(ICommitRelease owner, Object target)> _entries = new(4);

    public void Add(ICommitRelease owner, Object target) => _entries.Add((owner, target));

    internal void ReleaseAll()
    {
        for (int i = 0; i < _entries.Count; i++)
            _entries[i].owner.Release(_entries[i].target);

        _entries.Clear();
    }
}

/// <summary>확정 때 풀어 줄 예약을 가진 쪽.</summary>
public interface ICommitRelease
{
    void Release(Object target);
}
