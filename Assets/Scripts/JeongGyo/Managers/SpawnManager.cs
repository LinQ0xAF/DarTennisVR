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

    public void SpawnForClient(ulong clientId, int playerOrderIndex)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("SpawnManager: spawnPoints가 비어 있습니다.", this);
            return;
        }

        // 1. 호출 측(MatchManager)이 전달한 순번을 기준으로 포인트/플립 결정
        int playerIndex = playerOrderIndex;

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
