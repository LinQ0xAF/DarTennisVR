using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class NetworkDartHandler : NetworkBehaviour
{
    [Header("Settings")]
    public Transform HandAttachPoint; // 다트 생성 위치
    public InputActionReference ReloadAction; // Grip 버튼
    public DartSettingsSO Settings;

    [Header("References")]
    public LocalDartPool LocalPool; // 로컬 풀 참조
    private PlayerDartState _networkState; // 내 네트워크 상태 변수
    private NetworkDartPoolManager _serverPoolManager; // 서버 풀 매니저 (참조용)

    private void Start()
    {
        if (IsOwner)
        {
            _serverPoolManager = FindFirstObjectByType<NetworkDartPoolManager>();
            // 내 NetworkPlayer(아바타)를 찾아서 State 스크립트 가져오기
            var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
            _networkState = playerObj.GetComponent<PlayerDartState>();
        }
    }

    private void OnEnable()
    {
        if (IsOwner && ReloadAction != null)
            ReloadAction.action.performed += OnReloadInput;
    }

    private void OnDisable()
    {
        if (ReloadAction != null) ReloadAction.action.performed -= OnReloadInput;
    }

    // 1. 장전 (로컬 전용)
    private void OnReloadInput(InputAction.CallbackContext ctx)
    {
        if (!IsOwner || _networkState == null) return;

        // 이미 들고 있으면 패스 (단순화)
        if (_networkState.IsHoldingDart.Value) return;

        // A. 로컬 다트 생성 (XRI)
        GameObject dartObj = LocalPool.Pool.Get();
        dartObj.transform.position = HandAttachPoint.position;
        dartObj.transform.rotation = HandAttachPoint.rotation;

        // B. 초기화 및 XRI 강제 잡기
        var localDart = dartObj.GetComponent<LocalDart>();
        if (localDart != null)
        {
            localDart.Init(this, LocalPool.Pool);
            // (여기서 XRI interactionManager.SelectEnter 호출하여 손에 쥐어줌)
        }

        // C. 네트워크 상태 변경 (나는 다트를 들었다!)
        // -> 이 변수가 바뀌면 상대방 화면에 내 손에 껍데기 다트가 생김
        UpdateHandStateServerRpc(true);
    }

    // 2. 던지기 요청 (LocalDart가 호출)
    public void RequestThrow(Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angVel)
    {
        if (!IsOwner) return;

        // A. 상태 변경 (손에서 떠남)
        UpdateHandStateServerRpc(false);

        // B. 서버에 진짜 투사체 발사 요청
        SpawnProjectileServerRpc(pos, rot, vel * Settings.ThrowPowerMultiplier, angVel);
    }

    [ServerRpc]
    private void UpdateHandStateServerRpc(bool isHolding)
    {
        if (_networkState != null)
        {
            _networkState.IsHoldingDart.Value = isHolding;
        }
    }

    [ServerRpc]
    private void SpawnProjectileServerRpc(Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angVel)
    {
        if (_serverPoolManager != null)
        {
            // OwnerClientId를 넘겨서, 던진 본인 화면에선 안 보이게 처리
            _serverPoolManager.Server_SpawnProjectile(OwnerClientId, pos, rot, vel, angVel);
        }
    }
}