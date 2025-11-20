using Unity.Netcode;
using UnityEngine;
using Unity.XR.CoreUtils; // XR Origin 찾기 위해 필요

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

    public override void OnNetworkSpawn() //
    {
        // 오직 서버(호스트)만 이 이벤트를 구독합니다.
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    // 클라이언트가 접속했을 때 서버에서 실행되는 함수
    private void OnClientConnected(ulong clientId)
    {
        // 1. 몇 번째 접속자인지 확인 (0번: 1P, 1번: 2P ...)
        // (전용 서버의 경우 서버 자신은 플레이어가 아니므로 리스트 카운트로 계산)
        int playerIndex = NetworkManager.Singleton.ConnectedClientsIds.Count - 1;
        
        // 예외 처리: 스폰 포인트보다 사람이 많으면 0번으로
        if (playerIndex >= spawnPoints.Length) playerIndex = 0;

        Transform spawnPoint = spawnPoints[playerIndex];

        // 2. [서버 -> 클라이언트] "너의 XR Origin을 저 위치로 옮겨라" 명령
        // ClientRpcParams를 사용하여 '해당 클라이언트에게만' 전송
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
        };

        TeleportClient_ClientRpc(spawnPoint.position, spawnPoint.rotation, clientRpcParams);

        // 3. [서버] 해당 위치에 NetworkPlayer(아바타) 생성 및 소유권 부여
        // (아바타는 생성되자마자 주인의 XR Origin 위치로 텔레포트 될 것입니다)
        NetworkObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        playerInstance.SpawnAsPlayerObject(clientId, true);
    }

    [ClientRpc]
    private void TeleportClient_ClientRpc(Vector3 pos, Quaternion rot, ClientRpcParams rpcParams = default)
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
}