using UnityEngine;
using UnityEngine.Pool;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Dart : MonoBehaviour
{
    // 이 다트가 속한 오브젝트 풀
    public IObjectPool<GameObject> Pool { get; set; }

    [Tooltip("다트가 던져진 후 풀에 자동으로 반환될 때까지의 최대 시간 (초)")]
    public float MaxLifetime = 10.0f;

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
        _GrabInteractable.selectExited.AddListener(OnThrown);
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
        _GrabInteractable.selectExited.RemoveListener(OnThrown);
    }

    private void OnThrown(UnityEngine.XR.Interaction.Toolkit.SelectExitEventArgs args)
    {
        // 다트가 던져졌을 때(플레이어의 손을 떠났을 때) 최대 수명 타이머 시작
        _MaxLifetimeCoroutine = StartCoroutine(ReturnToPoolAfterMaxLifetime());
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
        if (Pool != null && gameObject.activeInHierarchy)
        {
            Pool.Release(gameObject);
        }
    }
}