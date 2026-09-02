/// <summary>
/// 수동 시간 기능을 지원하는 Option만 선택적으로 구현하는 Debug 조작 계약입니다.
/// </summary>
public interface IManualTimeDebugCommand
{
    void UseSystemTime();
    void UseManualTime();
    void AddGameDays(int days);
    void ResetManualTime();
}
