using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class LocalDart : MonoBehaviour
{
    private LocalDartPool _LocalDartPoolManager;
    private XRGrabInteractable _interactable;
    private Rigidbody _rb;
    private NetworkDartThrowHandler _currentHandler; // 나를 잡은 주손 핸들러
    private Coroutine _ReturnCoroutine;

    private void Awake()
    {
        _interactable = GetComponent<XRGrabInteractable>();
        _rb = GetComponent<Rigidbody>();
    }

    // 풀 매니저 설정 (생성 시 1회 호출)
    public void SetPoolManager(LocalDartPool manager)
    {
        _LocalDartPoolManager = manager;
    }

    private void OnEnable()
    {
        // 초기화
        _currentHandler = null;
        
        _interactable.enabled = true;
        _rb.isKinematic = false; 
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        _interactable.selectEntered.AddListener(OnGrabbed);
        _interactable.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        _interactable.selectEntered.RemoveListener(OnGrabbed);
        _interactable.selectExited.RemoveListener(OnReleased);
        StopAllCoroutines(); // 실행 중인 반납 타이머 정지
    }

    // 주손 핸들러 등록 (주손이 잡았을 때 DartThrowHandler가 호출)
    public void SetMainHandHandler(NetworkDartThrowHandler handler)
    {
        _currentHandler = handler;
    }

    // 잡혔을 때 (소켓 or 주손)
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // 다트가 잡혔을 때 진행중인 코루틴이 있다면 중지
        if (_ReturnCoroutine != null)
        {
            StopCoroutine(_ReturnCoroutine);
            _ReturnCoroutine = null;
        }
        // 소켓이 잡았을 땐 핸들러가 없으므로 아무 일도 안 일어남.
        // 주손이 잡았을 땐 DartThrowHandler가 SetMainHandHandler를 호출해 줄 것임.
    }

    // 놓였을 때
    private void OnReleased(SelectExitEventArgs args)
    {
        // 주손에서 놓여진 경우에만 처리
        if (_currentHandler != null)
        {
            var handlerTemp = _currentHandler;
            _currentHandler = null;
            StartCoroutine(CaptureVelocityNextFrame((vel, angVel) =>
            {
                handlerTemp.HandleDartRelease(this, vel, angVel);
            }));

            // 반납 시간을 5초로 늘려 충돌 시점까지 유지되도록 함
            _ReturnCoroutine = StartCoroutine(ReturnToPoolDelay(5.0f));
        }
        else
        {
            // 소켓에서 강제 드롭된 경우 (부손 놓기)
            if (_ReturnCoroutine != null)
            {
                StopCoroutine(_ReturnCoroutine);
            }
            _ReturnCoroutine = StartCoroutine(ReturnToPoolDelay(5.0f));
        }
    }

    // [추가] 로컬 충돌 처리: 벽에 닿으면 즉시 반납
    private void OnCollisionEnter(Collision collision)
    {
        // 이미 잡혀있거나 키네마틱이면 무시
        if (_interactable.isSelected || _rb.isKinematic) return;

        if (collision.gameObject.CompareTag("Environment"))
        {
            _rb.isKinematic = true;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

            ForceReturnToPool();
        }
    }

    // [추가] 외부(NetworkDart)에서 강제로 반납시키기 위한 함수
    public void ForceReturnToPool()
    {
        if (_ReturnCoroutine != null) StopCoroutine(_ReturnCoroutine);
        ReturnToPool();
    }

    private IEnumerator CaptureVelocityNextFrame(System.Action<Vector3, Vector3> callback)
    {
        yield return new WaitForFixedUpdate(); // 한 프레임 대기

        Vector3 vel = _rb.linearVelocity;
        Vector3 angVel = _rb.angularVelocity;

        callback?.Invoke(vel, angVel);
    }

    private IEnumerator ReturnToPoolDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (_LocalDartPoolManager != null)
        {
            _LocalDartPoolManager.ReleaseDart(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}