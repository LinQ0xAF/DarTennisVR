using UnityEngine;
using UnityEngine.Pool;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class Dart : MonoBehaviour
{
    [Tooltip("다트가 던져진 후 풀에 자동으로 반환될 때까지의 최대 시간 (초)")]
    public float MaxLifetime = 10.0f;

    private DartPoolManager _DartPoolManager;

    private Rigidbody _DartRigidbody;
    private XRGrabInteractable _GrabInteractable;
    private bool _HasCollided = false;
    private Coroutine _ReturnCoroutine;
    private Coroutine _MaxLifetimeCoroutine;

    private void Awake()
    {
        _DartRigidbody = GetComponent<Rigidbody>();
        _GrabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        // 재사용될 때 상태 초기화
        _HasCollided = false;
        _DartRigidbody.isKinematic = false;
        _DartRigidbody.linearVelocity = Vector3.zero;
        _DartRigidbody.angularVelocity = Vector3.zero;

        _GrabInteractable.throwOnDetach = true;

        // 이벤트 리스너 등록
        _GrabInteractable.selectEntered.AddListener(OnGrabbed);
        _GrabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        // 비활성화될 때 진행중인 코루틴이 있다면 중지
        if (_ReturnCoroutine != null)
        {
            StopCoroutine(_ReturnCoroutine);
            _ReturnCoroutine = null;
        }
        if (_MaxLifetimeCoroutine != null)
        {
            StopCoroutine(_MaxLifetimeCoroutine);
            _MaxLifetimeCoroutine = null;
        }

        // 이벤트 리스너 해제
        _GrabInteractable.selectEntered.RemoveListener(OnGrabbed);
        _GrabInteractable.selectExited.RemoveListener(OnReleased);
    }

    public void SetPoolManager(DartPoolManager poolManager)
    {
        _DartPoolManager = poolManager;
    }

    private void OnReleased(UnityEngine.XR.Interaction.Toolkit.SelectExitEventArgs args)
    {
        // 세상에 놓여졌으므로 '분실 위험' 상태로 간주하고 분실 타이머를 시작합니다.
        // 이전에 실행되던 타이머가 있다면 중복 실행을 방지하기 위해 먼저 중지합니다.
        if (_MaxLifetimeCoroutine != null)
        {
            StopCoroutine(_MaxLifetimeCoroutine);
        }
        _MaxLifetimeCoroutine = StartCoroutine(ReturnToPoolAfterMaxLifetime());
    }

    private void OnGrabbed(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        // 다트가 잡혔을 때 진행중인 코루틴이 있다면 중지
        if (_ReturnCoroutine != null)
        {
            StopCoroutine(_ReturnCoroutine);
            _ReturnCoroutine = null;
        }
        if (_MaxLifetimeCoroutine != null)
        {
            StopCoroutine(_MaxLifetimeCoroutine);
            _MaxLifetimeCoroutine = null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_HasCollided) return;

        // "Environment" 태그를 가진 오브젝트와 충돌했을 때
        if (collision.gameObject.CompareTag("Environment"))
        {
            _HasCollided = true;
            _DartRigidbody.isKinematic = true;

            // 최대 수명 코루틴이 실행중이라면 중지 (정상 충돌했으므로)
            if (_MaxLifetimeCoroutine != null)
            {
                StopCoroutine(_MaxLifetimeCoroutine);
                _MaxLifetimeCoroutine = null;
            }

            // 2초 후에 풀에 반납하는 코루틴 시작
            _ReturnCoroutine = StartCoroutine(ReturnToPoolAfterDelay(2.0f));
        }
    }

    private IEnumerator ReturnToPoolAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool();
    }

    private IEnumerator ReturnToPoolAfterMaxLifetime()
    {
        yield return new WaitForSeconds(MaxLifetime);
        
        // 이 코루틴이 끝까지 실행되었다는 것은 다트가 어딘가에서 분실되었음을 의미
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        // 풀이 할당되어 있고, 게임 오브젝트가 아직 활성 상태일 때만 반납 시도
        if (_DartPoolManager != null && gameObject.activeInHierarchy)
        {
            _DartPoolManager.ReleaseDart(gameObject);
        }
    }
}