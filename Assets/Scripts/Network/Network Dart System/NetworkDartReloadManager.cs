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
    [Header("Dart Reload Settings")]
    public InputActionReference GripAction;
    public GameObject DartPrefab;
    public string ReloadZoneTag = "ReloadZone";
    public bool IsRightHand = false;
    [SerializeField] private float SocketReactivationDelay = 0.8f;
    public List<XRSocketInteractor> DartSockets;
    [SerializeField] private DartSpawnChannelSO _SpawnChannel;
    private XRInteractionManager _InteractionManager;
    private bool _IsInReloadZone = false;

    void Awake()
    {
        _InteractionManager = FindFirstObjectByType<XRInteractionManager>();
    }

    private void OnEnable()
    {
        if (GripAction != null)
        {
            GripAction.action.performed += OnGripPressed;
            GripAction.action.canceled += OnGripReleased;
        }
    }

    private void OnDisable()
    {
        if (GripAction != null)
        {
            GripAction.action.performed -= OnGripPressed;
            GripAction.action.canceled -= OnGripReleased;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(ReloadZoneTag))
        {
            _IsInReloadZone = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(ReloadZoneTag))
        {
            _IsInReloadZone = false;
        }
    }


    private void OnGripPressed(InputAction.CallbackContext context)
    {
        if (_IsInReloadZone)
        {
            StartCoroutine(AttemptReloadCoroutine());
        }
    }

    private void OnGripReleased(InputAction.CallbackContext context)
    {
        ForceDropAllDarts();
    }

    private IEnumerator AttemptReloadCoroutine()
    {
        // 채널이 없으면 에러
        if (_SpawnChannel == null)
        {
            Debug.LogError("DartSpawnChannelSO is missing!");
            yield break;
        }

        foreach (var socket in DartSockets)
        {
            // 소켓이 비어있을 때만 요청
            if (socket != null && !socket.hasSelection)
            {
                // [핵심 변경]
                // 직접 Instantiate 하지 않고, 이벤트 채널에 요청을 보냅니다.
                // (서버의 DartPoolManager가 이 이벤트를 듣고 스폰해줄 것입니다.)
                
                _SpawnChannel.RaiseEvent(
                    NetworkManager.Singleton.LocalClientId, // 내 ID
                    socket.transform.position,              // 소켓 위치
                    socket.transform.rotation,              // 소켓 회전
                    IsRightHand                             // 어느 손인지
                );

                // 서버 처리를 위해 약간의 텀을 둠 (선택사항, 너무 빠른 연속 요청 방지)
                yield return null; 
            }
        }
    }

    private void ForceDropAllDarts()
    {
        if (_InteractionManager == null) return;
        foreach (var socket in DartSockets)
        {
            if (socket == null || !socket.hasSelection)
            {
                continue;
            }
        
            IXRSelectInteractable heldDartInteractableInterface = socket.firstInteractableSelected;
            if (heldDartInteractableInterface == null)
            {
                continue;
            }
            // XRGrabInteractable heldDart = heldDartInteractableInterface as XRGrabInteractable;

            // // 안전하게 Rigidbody 컴포넌트 가져오기
            // Rigidbody dartRigidbody = heldDart.GetComponent<Rigidbody>();

            // // 소켓에서 해제되었을 때 던져지지 않도록 Throw on Detach 비활성화
            // heldDart.throwOnDetach = false;

            // 1. 소켓을 일시적으로 비활성화하여 즉시 재장착되는 것을 방지
            socket.socketActive = false;

            // 잠재적인 문제 방지를 위해 interaction 종료 호출
            if (socket.isPerformingManualInteraction)
            {
                socket.EndManualInteraction();
            }
            else
            {
                _InteractionManager.SelectExit(socket, heldDartInteractableInterface);
            }

            // if (dartRigidbody != null)
            // {
            //     dartRigidbody.isKinematic = false;
            //     dartRigidbody.useGravity = true;
            // }

            // 2. 짧은 시간 후에 소켓을 다시 활성화하는 코루틴 시작
            StartCoroutine(ReactivateSocket(socket, SocketReactivationDelay));
        }
    }

    // 소켓을 일정 시간 후에 다시 활성화하는 코루틴
    private IEnumerator ReactivateSocket(XRSocketInteractor socket, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (socket != null)
        {
            socket.socketActive = true;
        }
    }
}
