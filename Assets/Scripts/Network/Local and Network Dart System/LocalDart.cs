using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class LocalDart : MonoBehaviour
{
    private IObjectPool<GameObject> _myPool;
    private XRGrabInteractable _interactable;
    private Rigidbody _rb;
    private NetworkDartHandler _handler;

    private void Awake()
    {
        _interactable = GetComponent<XRGrabInteractable>();
        _rb = GetComponent<Rigidbody>();
    }

    public void Init(NetworkDartHandler handler, IObjectPool<GameObject> pool)
    {
        _handler = handler;
        _myPool = pool;
        _interactable.enabled = true;
        _rb.isKinematic = false;

        _interactable.selectExited.AddListener(OnThrown);
    }

    private void OnDisable()
    {
        _interactable.selectExited.RemoveListener(OnThrown);
    }

    private void OnThrown(SelectExitEventArgs args)
    {
        // 1. 물리 정보 캡처
        // (Unity 6: linearVelocity, 이전: velocity)
        Vector3 vel = _rb.linearVelocity; 
        Vector3 angVel = _rb.angularVelocity;

        // 2. 핸들러에게 처리 위임
        if (_handler != null)
        {
            _handler.RequestThrow(transform.position, transform.rotation, vel, angVel);
        }

        // 3. 로컬 잔해 처리 (다시 못 잡게 하고 잠시 뒤 사라짐)
        _interactable.enabled = false;
        StartCoroutine(ReturnDelay());
    }

    private IEnumerator ReturnDelay()
    {
        // 잔상이 남도록 3초 뒤 반납 (그 사이엔 물리 엔진으로 날아감)
        yield return new WaitForSeconds(3.0f);
        if (_myPool != null) _myPool.Release(gameObject);
    }
}