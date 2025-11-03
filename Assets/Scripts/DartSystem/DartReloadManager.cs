using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.Pool;

public class DartReloadManager : MonoBehaviour
{
    public InputActionReference GripAction;
    public GameObject DartPrefab;
    public string ReloadZoneTag = "ReloadZone";
    public List<XRSocketInteractor> DartSockets;
    private XRInteractionManager _InteractionManager;
    private bool _IsInReloadZone = false;
    [SerializeField] private float SocketReactivationDelay = 0.8f;

    public int InitialPoolSize = 10;
    public int MaxPoolSize = 20;

    private IObjectPool<GameObject> _DartPool;

    void Awake()
    {
        _InteractionManager = FindFirstObjectByType<XRInteractionManager>();

        // ObjectPool 초기화
        _DartPool = new ObjectPool<GameObject>(
            createFunc: CreateDart,
            actionOnGet: OnGetFromPool,
            actionOnRelease: OnReleaseToPool,
            actionOnDestroy: OnDestroyInPool,
            collectionCheck: true, // 중복 반납 체크
            defaultCapacity: InitialPoolSize,
            maxSize: MaxPoolSize
        );
    }

    #region Pool_Callbacks
    private GameObject CreateDart()
    {
        GameObject dartInstance = Instantiate(DartPrefab);
        // 생성된 다트에게 자신이 속한 풀을 알려줌
        dartInstance.GetComponent<Dart>().Pool = _DartPool;
        return dartInstance;
    }

    private void OnGetFromPool(GameObject dart)
    {
        dart.SetActive(true);
    }

    private void OnReleaseToPool(GameObject dart)
    {
        dart.SetActive(false);
    }

    private void OnDestroyInPool(GameObject dart)
    {
        Destroy(dart);
    }
    #endregion

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
            AttemptReload();
        }
    }

    private void OnGripReleased(InputAction.CallbackContext context)
    {
        ForceDropAllDarts();
    }

    private void AttemptReload()
    {
        foreach (var socket in DartSockets)
        {
            // 리스트에 null 요소가 있는 경우를 대비
            if (socket == null || socket.hasSelection)
            {
                continue;
            }
            
            // 오브젝트 풀에서 다트 인스턴스 받아오기
            GameObject newDart = _DartPool.Get();
            newDart.transform.position = socket.transform.position;
            newDart.transform.rotation = socket.transform.rotation;
            //GameObject newDart = Instantiate(DartPrefab, socket.transform.position, socket.transform.rotation);
            
            IXRSelectInteractable newDartInteractable = newDart.GetComponent<IXRSelectInteractable>();
            if (newDartInteractable != null)
            {
                socket.StartManualInteraction(newDartInteractable);
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
            XRGrabInteractable heldDart = heldDartInteractableInterface as XRGrabInteractable;

            // 안전하게 Rigidbody 컴포넌트 가져오기
            Rigidbody dartRigidbody = heldDart.GetComponent<Rigidbody>();

            // 소켓에서 해제되었을 때 던져지지 않도록 Throw on Detach 비활성화
            heldDart.throwOnDetach = false;

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

            if (dartRigidbody != null)
            {
                dartRigidbody.isKinematic = false;
                dartRigidbody.useGravity = true;
            }

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
