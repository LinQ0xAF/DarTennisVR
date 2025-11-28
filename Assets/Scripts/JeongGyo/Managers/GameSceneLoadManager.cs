using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 싱글/로컬 플레이용 씬 로드 매니저. RoomConfigDto를 static으로 저장해 다음 씬에서 소비한다.
/// </summary>
public class GameSceneLoadManager : MonoBehaviour
{
    private static RoomConfigDto pendingConfig;

    /// <summary>대기 중인 RoomConfigDto 가져오기.</summary>
    public static bool TryConsumePendingConfig(out RoomConfigDto config)
    {
        if (pendingConfig == null)
        {
            config = null;
            return false;
        }

        config = pendingConfig;
        pendingConfig = null;
        return true;
    }

    /// <summary>싱글/로컬 게임 씬 로드.</summary>
    public void LoadGameScene(RoomConfigDto config)
    {
        if (config == null)
        {
            Debug.LogError("GameSceneLoadManager: 전달받은 RoomConfigDto 가 null 입니다.", this);
            return;
        }

        pendingConfig = config;
        LoadSceneInternal(pendingConfig.gamePlaySceneName);
    }

    private void LoadSceneInternal(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("GameSceneLoadManager: 로드할 씬 이름이 비어 있습니다.", this);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
