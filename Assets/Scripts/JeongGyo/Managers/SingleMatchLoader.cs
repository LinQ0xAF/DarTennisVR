using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

/// <summary>
/// 싱글/로컬 플레이용 씬 로드 매니저. RoomConfigDto를 static으로 저장해 다음 씬에서 소비한다.
/// </summary>
public class SingleMatchLoader : MonoBehaviour
{
    [SerializeField] private RoomConfigSO roomConfig; // 프리셋+런타임 상태를 함께 들고 있는 SO

    /// <summary>싱글/로컬 게임 씬 로드.</summary>
    public void LoadSingleGame(RoomConfigDto config, string gamePlaySceneName)
    {
        if (config == null || string.IsNullOrWhiteSpace(gamePlaySceneName))
        {
            Debug.LogError("GameSceneLoadManager: 전달받은 RoomConfigDto 가 null 입니다.", this);
            return;
        }

         //pendingConfig = config;
        if (roomConfig != null)
            roomConfig.SetRuntime(config); 
        // 멀티플레이와 마찬가지로 동일한 so로부터 복제된 so를 사용함 -> 런타임 dto랑 겹치지 않는지 확인해야함.

        // 싱글 모드 진입 전에 네트워크 매니저를 정지/비활성화한다.
        var networkManager = NetworkManager.Singleton;
        if (networkManager != null)
        {
            if (networkManager.IsListening)
                networkManager.Shutdown();

            if (networkManager.gameObject != null)
                networkManager.gameObject.SetActive(false);
        }

        SceneManager.LoadScene(gamePlaySceneName);
     
    }

 
}
