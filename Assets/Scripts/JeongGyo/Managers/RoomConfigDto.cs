using System;

/// <summary>
/// 멀티 매치 설정 DTO. Netcode RPC는 프리미티브로 전송하고, 각 클라이언트에서 이 DTO로 조립해 사용한다.
/// </summary>
[Serializable]
public class RoomConfigDto
{
    public string gamePlaySceneName;
    public int balloonCount;
    public int timeLimitSeconds;
    public int setCount;
}
