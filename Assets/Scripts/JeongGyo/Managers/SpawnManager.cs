using Unity.Netcode;
using UnityEngine;
using Unity.XR.CoreUtils; // XR Origin 찾기 위해 필요
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Spawn Points List")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Player Prefab NetworkObject")]
    [SerializeField] private NetworkObject playerPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    public override void OnNetworkSpawn() // 네트워크 오브젝트가 스폰될 때 호출
    {
        // 씬 로드 완료 시점에 이미 접속해 있는 클라이언트들을 처리한다.
        SubscribeSceneLoaded();
    }

    public override void OnNetworkDespawn() // 네트워크 오브젝트가 언스폰될 때 호출
    {
        UnsubscribeSceneLoaded();
    }

    private void SubscribeSceneLoaded()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;

        nm.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
    }

    private void UnsubscribeSceneLoaded()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;

        nm.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
    }

    // 씬 로드가 완료된 후 서버에서 이미 접속해 있는 클라이언트들을 스폰 처리, 매개변수 자체는 이미 OnLoadEventCompleted 델리게이트에 정의된 형태로 고정
    private void OnSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer) return;

        foreach (var clientId in clientsCompleted)
        {
            SpawnForClient(clientId);
        }
    }

    private void SpawnForClient(ulong clientId)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("SpawnManager: spawnPoints가 비어 있습니다.", this);
            return;
        }

        // 1. 몇 번째 접속자인지 확인 (0번: 1P, 1번: 2P ...)
        // ConnectedClientsIds 순서를 기준으로 매핑(접속 순서대로 0,1,...)
        int playerIndex = 0;
        var ids = NetworkManager.Singleton.ConnectedClientsIds;
        for (int i = 0; i < ids.Count; i++)
        {
            if (ids[i] == clientId)
            {
                playerIndex = i;
                break;
            }
        }

        // 스폰 포인트 수(2)에 맞춰 모듈러 적용
        playerIndex = playerIndex % spawnPoints.Length;
        bool shouldFlipUi = (playerIndex % 2) == 1; // 2P 이상은 UI 반전

        Transform spawnPoint = spawnPoints[playerIndex];

        // 2. [서버 -> 클라이언트] "너의 XR Origin을 저 위치로 옮겨라" 명령
        // ClientRpcParams를 사용하여 '해당 클라이언트에게만' 전송
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
        };

        TeleportClient_ClientRpc(spawnPoint.position, spawnPoint.rotation, clientRpcParams);
        SetPlayerUiOrientation_ClientRpc(shouldFlipUi, clientRpcParams);

        // 3. [서버] 해당 위치에 NetworkPlayer(아바타) 생성 및 소유권 부여
        // (아바타는 생성되자마자 주인의 XR Origin 위치로 텔레포트 될 것입니다)
        NetworkObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        playerInstance.SpawnAsPlayerObject(clientId, true);
    }

    [ClientRpc]
    private void TeleportClient_ClientRpc(Vector3 pos, Quaternion rot, ClientRpcParams rpcParams = default) // 클라이언트에게 자신의 XR Origin을 이동시키라는 명령
    {
        // 4. [클라이언트] 명령을 받으면 자신의 XR Origin을 찾아서 이동
        XROrigin xrOrigin = FindFirstObjectByType<XROrigin>();
        
        if (xrOrigin != null)
        {
            // XR Origin 루트 이동
            xrOrigin.transform.position = pos;
            
            // 회전: XR Origin을 돌리거나, CameraOffset을 조정하여 시선 맞추기
            // (간단하게는 루트 회전)
            xrOrigin.transform.rotation = rot;
            
            // (선택) 카메라 Y축 회전 보정 로직이 필요할 수 있음 (MatchOrientation)
            Debug.Log($"[GameManager] Spawned at {pos}");
        }
    }

    [ClientRpc]
    private void SetPlayerUiOrientation_ClientRpc(bool flipForThisClient, ClientRpcParams rpcParams = default)
    {
        var flipper = FindFirstObjectByType<UIOrientationFlipper>();
        if (flipper != null)
        {
            flipper.SetFlipped(flipForThisClient);
        }
    }
}
