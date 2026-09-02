/// <summary>
/// Option별 Runtime 상태를 공통 Debug Snapshot으로 제공하는 읽기 전용 계약입니다.
/// </summary>
public interface ITimeArchitectureDebugSource
{
    TimeArchitectureDebugSnapshot GetSnapshot();
}
