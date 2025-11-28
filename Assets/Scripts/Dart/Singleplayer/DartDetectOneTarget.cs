using UnityEngine;
using UnityEngine.EventSystems;

public class DartDetectOneTarget : MonoBehaviour
{
    [SerializeField]
    private string MyTargetObjTag = "Balloons";

    private string _initialTargetTag;
    private bool _hasHit = false; // 풀링된 다트가 한 번만 맞도록 제어
    private Balloons CollisionBalloons; // 충돌된 풍선 오브젝트의 Balloons 스크립트를 참조하기 위한 변수

    private void Awake()
    {
        _initialTargetTag = MyTargetObjTag;
    }

    private void OnEnable()
    {
        // 풀 재사용 시 상태 리셋
        _hasHit = false;
        MyTargetObjTag = _initialTargetTag;
        CollisionBalloons = null;
    }

    void OnTriggerEnter(Collider other)
    {  
        if (_hasHit || string.IsNullOrEmpty(MyTargetObjTag))
            return;

        Debug.Log($"Collision detected with {other.gameObject.name}");
        
        CollisionBalloons = other.gameObject.GetComponentInParent<Balloons>(); // 충돌된 오브젝트(hitBox 오브젝트)의 부모 오브젝트에서 Balloons 스크립트를 찾음
        
        if (other.CompareTag(MyTargetObjTag)) //타켓 오브젝트로 지정한 태그와 충돌된 오브젝트의 태그가 일치한다면 
        {
            _hasHit = true; // 한 번만 처리
            CollisionBalloons.HitBalloon();
        }
    }

}
