using System;
using UnityEngine;

[Serializable]
public class RoomConfigDto
{
    public int balloonCount;
    public int timeLimitSeconds;
    public int setCount;
}

/// <summary>
/// 에디터 프리셋 + 런타임 상태를 함께 들고 있는 통합형 SO.
/// 네트워크 전송 시에는 ToDtoFromPreset()으로 DTO를 만들어 사용하고,
/// 런타임 수신 값은 [NonSerialized] 필드에 저장한다.
/// </summary>
[CreateAssetMenu(fileName = "RoomConfig", menuName = "Configs/RoomConfig")]
public class RoomConfigSO : ScriptableObject
{
    [Header("Gameplay Preset")]
    [Range(1, 5)] public int balloonCount = 1;
    public int timeLimitSeconds = 60;
    public int setCount = 1;

    [Header("Runtime (not saved)")]
    [NonSerialized] public RoomConfigDto runtimeConfig;

    private void OnEnable()
    {
        ResetRuntime(); // 플레이 시작 시 깨끗하게 초기화
    }

    /// <summary>
    /// 프리셋 값을 네트워크 전송 가능한 DTO로 복사한다.
    /// </summary>
    public RoomConfigDto ToDtoFromPreset()
    {
        return new RoomConfigDto
        {
            balloonCount = balloonCount,
            timeLimitSeconds = timeLimitSeconds,
            setCount = setCount
        };
    }

    /// <summary>
    /// 네트워크로 받은 설정을 런타임 상태에 저장한다.
    /// </summary>
    public void SetRuntime(RoomConfigDto cfg)
    {
        runtimeConfig = cfg;
    }

    public void ResetRuntime()
    {
        runtimeConfig = null;
    }
}
