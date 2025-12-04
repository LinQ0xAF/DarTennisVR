using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.Netcode;

/// <summary>
/// 멀티플레이용 다트 차징 핸들러
/// </summary>
public class NetworkDartThrowHandler : DartThrowHandlerBase
{
    private LocalDart _HeldDart;
    private PlayerNetworkBridge _Bridge;

    protected override void Awake()
    {
        base.Awake();
    }

    void Start()
    {
        InitializeBridge();
    }

    private void InitializeBridge()
    {
        if (_Bridge == null && NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient?.PlayerObject != null)
        {
            _Bridge = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetworkBridge>();
        }
    }

    protected override bool IsValidDart(IXRSelectInteractable interactable)
    {
        // 멀티플레이: LocalDart 컴포넌트가 있는지 확인
        return interactable.transform.TryGetComponent<LocalDart>(out _);
    }

    protected override void OnValidDartGrabbed(SelectEnterEventArgs args)
    {
        if (args.interactableObject.transform.TryGetComponent<LocalDart>(out var dart))
        {
            _HeldDart = dart;
            
            // 다트에게 나(주손)에게 잡혔음을 알림
            dart.SetMainHandHandler(this);

            // 네트워크 상태 업데이트: 부손 개수 -1, 손에 듦 = true
            if (_Bridge == null) InitializeBridge();
            
            if (_Bridge != null) 
            {
                _Bridge.UpdateOffHandDarts(-1);
                _Bridge.UpdateHoldingState(true);
            }
        }
    }

    protected override void OnValidDartReleased(SelectExitEventArgs args)
    {
        _HeldDart = null;
    }

    protected override void OnChargingStarted()
    {
        // TODO: 차징 이펙트/사운드 재생
        // Debug.Log("Charging Started");
    }

    protected override void OnChargingEnded()
    {
        // TODO: 차징 종료 이펙트/사운드
    }

    // --- LocalDart가 호출하는 결과 처리 ---
    public void HandleDartRelease(LocalDart dart, Vector3 vel, Vector3 angVel)
    {
        if (_Bridge == null) InitializeBridge();

        // 네트워크 상태 업데이트: 손에 듦 = false
        if (_Bridge != null) 
        {
            _Bridge.UpdateHoldingState(false);
            _Bridge.RequestSpawnProjectile(dart.transform.position, dart.transform.rotation, vel, angVel);
        }
    }
}
