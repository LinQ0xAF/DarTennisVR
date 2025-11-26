using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.Pool;
using Unity.Netcode;

public class NetworkDartReloadManager : MonoBehaviour
{
    [Header("Input & Settings")]
    public InputActionReference GripAction;
    public string ReloadZoneTag = "ReloadZone";
    [SerializeField] private float SocketReactivationDelay = 0.5f;

    [Header("References")]
    public List<XRSocketInteractor> DartSockets;
    public LocalDartPool LocalPool; // 로컬 풀 참조 필수

    private bool _isInReloadZone = false;
    private PlayerNetworkBridge _bridge; // 네트워크 중계자
    private XRInteractionManager _interactionManager;

    void Start()
    {
        _interactionManager = FindFirstObjectByType<XRInteractionManager>();

        // 내 로컬 네트워크 플레이어(아바타)에서 Bridge 찾기
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient?.PlayerObject != null)
        {
            _bridge = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetworkBridge>();
        }
    }

    void OnEnable()
    {
        if (GripAction != null)
        {
            GripAction.action.performed += OnGripPressed;
            GripAction.action.canceled += OnGripReleased;
        }
    }

    void OnDisable()
    {
        if (GripAction != null)
        {
            GripAction.action.performed -= OnGripPressed;
            GripAction.action.canceled -= OnGripReleased;
        }
    }

    // --- 물리 트리거 (허리 감지) ---
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(ReloadZoneTag)) _isInReloadZone = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(ReloadZoneTag)) _isInReloadZone = false;
    }

    // --- 입력 핸들러 ---
    private void OnGripPressed(InputAction.CallbackContext ctx)
    {
        if (_isInReloadZone)
        {
            StartCoroutine(ReloadRoutine());
        }
    }

    private void OnGripReleased(InputAction.CallbackContext ctx)
    {
        ForceDropLocal();
    }

    // --- 장전 로직 ---
    private IEnumerator ReloadRoutine()
    {
        int addedCount = 0;

        foreach (var socket in DartSockets)
        {
            // 소켓이 존재하고 비어있을 때만 장전
            if (socket != null && !socket.hasSelection)
            {
                // 1. 로컬 풀에서 다트 꺼내기 (위치/회전 설정 포함)
                GameObject dartObj = LocalPool.GetDart(socket.transform.position, socket.transform.rotation);
                
                // 2. (삭제됨) LocalDart 초기화는 CreateDart 시점에 이미 수행됨

                // 3. XRI 소켓에 강제 장착
                var interactable = dartObj.GetComponent<IXRSelectInteractable>();
                if (interactable != null)
                {
                    socket.StartManualInteraction(interactable);
                }
                
                addedCount++;
                
                // 한 프레임에 하나씩 (선택사항, 자연스러움 위해)
                yield return null; 
            }
        }

        // 5. 네트워크 상태 업데이트 (허리에 다트 추가됨)
        if (_bridge != null && addedCount > 0)
        {
            _bridge.UpdateOffHandDarts(addedCount);
        }
    }

    // --- 드롭 로직 (Grip 뗐을 때) ---
    private void ForceDropLocal()
    {
        if (_interactionManager == null) return;

        foreach (var socket in DartSockets)
        {
            if (socket != null && socket.hasSelection)
            {
                var interactable = socket.firstInteractableSelected;
                
                // 소켓 비활성화 (즉시 재장착 방지)
                StartCoroutine(SocketCooldown(socket));

                // 강제 해제
                if (socket.isPerformingManualInteraction)
                    socket.EndManualInteraction();
                else
                    _interactionManager.SelectExit(socket, interactable);
                
                // (떨어진 다트는 LocalDart.OnThrown이 호출되지 않고, 
                //  LocalDart 내부 로직에 의해 3초 뒤 자동 반납됨)
            }
        }

        if (_bridge != null)
        {
            // 네트워크 상태 업데이트 (다트 반납됨)
            _bridge.UpdateOffHandDarts(-3);
        }
    }

    private IEnumerator SocketCooldown(XRSocketInteractor socket)
    {
        socket.socketActive = false;
        yield return new WaitForSeconds(SocketReactivationDelay);
        socket.socketActive = true;
    }
}
