using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;
using Unity.Netcode;

/// <summary>
/// 멀티플레이용 다트 리로드 매니저
/// </summary>
public class NetworkDartReloadManager : DartReloadManagerBase
{
    [Header("Pool Reference")]
    public LocalDartPool LocalPool;

    private PlayerNetworkBridge _bridge;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        // 초기화 시도 (실패해도 괜찮음, 사용할 때 다시 찾음)
        InitializeBridge();
    }

    private void InitializeBridge()
    {
        if (_bridge == null && NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient?.PlayerObject != null)
        {
            _bridge = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetworkBridge>();
        }
    }

    protected override void AttemptReload()
    {
        if (LocalPool == null)
        {
            Debug.LogError("LocalDartPool reference is missing!");
            return;
        }

        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        int addedCount = 0;

        foreach (var socket in DartSockets)
        {
            if (socket == null || socket.hasSelection) continue;

            // 풀에서 다트 꺼내기
            GameObject dartObj = LocalPool.GetDart(
                socket.transform.position,
                socket.transform.rotation
            );

            // 소켓에 장착
            var interactable = dartObj.GetComponent<IXRSelectInteractable>();
            if (interactable != null)
            {
                socket.StartManualInteraction(interactable);
            }

            addedCount++;

            // 한 프레임에 하나씩 (자연스러움)
            yield return null;
        }

        // 네트워크 상태 업데이트
        if (_bridge == null) InitializeBridge();
        
        if (_bridge != null && addedCount > 0)
        {
            _bridge.UpdateOffHandDarts(addedCount);
        }
    }

    protected override void OnAllDartsDropped()
    {
        // 네트워크 상태 업데이트 (다트 반납됨)
        if (_bridge == null) InitializeBridge();

        if (_bridge != null)
        {
            _bridge.UpdateOffHandDarts(-DartSockets.Count);
        }
    }
}
