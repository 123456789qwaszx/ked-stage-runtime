/// <summary>
/// 실행기가 재생 정책에서 받는 것은 하나 — 실행을 압축할지(트윈을 즉시 확정).
/// VN의 자동·속도·빨리 넘기기·시크 깃발은 여기로 오지 않는다: 재생 쪽이 이것으로 접어서 준다.
/// 속도 올리기는 타자기만 빠르게 하고 연출 길이는 건드리지 않는다 — 시간 배율은 받지 않는다.
/// </summary>
public interface ICommandPacePolicy
{
    bool CompressExecution { get; }
}
