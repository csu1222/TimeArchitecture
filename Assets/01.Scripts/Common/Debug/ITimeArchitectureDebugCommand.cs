/// <summary>
/// 모든 시간 아키텍처 데모에서 공통으로 제공하는 Debug 조작 계약입니다.
/// </summary>
public interface ITimeArchitectureDebugCommand
{
    void StartTrade();
    void ResetTrade();
    void SetTimeScale(float value);
}
