using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneLoadManager : MonoBehaviour
{
    [System.Serializable]
    public class RoomConfig
    {
        public string gamePlaySceneName;
        public int setIndex;
        public string setLabel;
        public int balloonCount;
        public int timeLimitSeconds;

    }

    private static RoomConfig pendingConfig;

       public static bool TryConsumePendingConfig(out RoomConfig config) // 대기중인 RoomConfig 가져오기
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

    public void LoadGameScene(RoomConfig config) // 게임 씬 로드
    {
        if (config == null)
        {
            Debug.LogError("GameSceneLoadManager: 전달받은 RoomConfig 가 null 입니다.", this);
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
