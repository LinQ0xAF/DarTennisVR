using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 다트 리로드 매니저의 공통 기능을 제공하는 추상 클래스
/// </summary>
public abstract class DartReloadManagerBase : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference GripAction;

    [Header("Settings")]
    public string ReloadZoneTag = "ReloadZone";
    [SerializeField] protected float socketReactivationDelay = 0.8f;

    [Header("References")]
    public List<XRSocketInteractor> DartSockets;

    protected XRInteractionManager interactionManager;
    protected bool isInReloadZone = false;

    protected virtual void Awake()
    {
        interactionManager = FindFirstObjectByType<XRInteractionManager>();
    }

    protected virtual void OnEnable()
    {
        if (GripAction != null)
        {
            GripAction.action.performed += OnGripPressed;
            GripAction.action.canceled += OnGripReleased;
        }
    }

    protected virtual void OnDisable()
    {
        if (GripAction != null)
        {
            GripAction.action.performed -= OnGripPressed;
            GripAction.action.canceled -= OnGripReleased;
        }
    }

    #region Trigger Detection
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(ReloadZoneTag))
        {
            isInReloadZone = true;
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(ReloadZoneTag))
        {
            isInReloadZone = false;
        }
    }
    #endregion

    #region Input Handlers
    protected virtual void OnGripPressed(InputAction.CallbackContext context)
    {
        if (isInReloadZone)
        {
            AttemptReload();
        }
    }

    protected virtual void OnGripReleased(InputAction.CallbackContext context)
    {
        ForceDropAllDarts();
    }
    #endregion

#region Core Logic
    /// <summary>
    /// 빈 소켓에 다트를 채우는 로직 (자식에서 구현)
    /// </summary>
    protected abstract void AttemptReload();

    /// <summary>
    /// 모든 소켓의 다트를 강제로 놓는 공통 로직
    /// </summary>
    protected virtual void ForceDropAllDarts()
    {
        if (interactionManager == null) return;

        foreach (var socket in DartSockets)
        {
            if (socket == null || !socket.hasSelection) continue;

            IXRSelectInteractable heldInteractable = socket.firstInteractableSelected;
            if (heldInteractable == null) continue;

            // 던지기 방지
            if (heldInteractable is XRGrabInteractable grabInteractable)
            {
                grabInteractable.throwOnDetach = false;

                // Rigidbody 설정
                Rigidbody dartRb = grabInteractable.GetComponent<Rigidbody>();
                if (dartRb != null)
                {
                    dartRb.isKinematic = false;
                    dartRb.useGravity = true;
                }
            }

            // 소켓 쿨다운 시작 (즉시 재장착 방지)
            StartCoroutine(SocketCooldown(socket));

            // 강제 해제
            if (socket.isPerformingManualInteraction)
            {
                socket.EndManualInteraction();
            }
            else
            {
                interactionManager.SelectExit(socket, heldInteractable);
            }
        }

        // 자식 클래스에서 추가 처리 (네트워크 상태 등)
        OnAllDartsDropped();
    }

    /// <summary>
    /// 소켓을 일정 시간 후 다시 활성화
    /// </summary>
    protected IEnumerator SocketCooldown(XRSocketInteractor socket)
    {
        socket.socketActive = false;
        yield return new WaitForSeconds(socketReactivationDelay);
        if (socket != null)
        {
            socket.socketActive = true;
        }
    }
#endregion

#region Virtual Hooks
    /// <summary>
    /// 모든 다트가 드롭되었을 때 호출 (자식에서 오버라이드)
    /// </summary>
    protected virtual void OnAllDartsDropped() { }
#endregion
}
