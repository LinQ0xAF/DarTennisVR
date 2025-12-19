using UnityEngine;
using Unity.Netcode;

public class NetworkManagerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _NetworkManagerPrefab; // 여기에 프리팹 연결

    private void Awake()
    {
        // 1. 현재 게임 전체에 NetworkManager가 존재하는지 확인
        if (NetworkManager.Singleton == null)
        {
            // 2. 없다면 생성
            GameObject nm = Instantiate(_NetworkManagerPrefab);
            
            Debug.Log("[Spawner] 새 NetworkManager를 생성했습니다.");
        }
        else
        {
            Debug.Log("[Spawner] 이미 NetworkManager가 존재하므로 생성하지 않습니다.");
        }
    }
}