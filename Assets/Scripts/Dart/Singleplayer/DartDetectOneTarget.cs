using UnityEngine;
using UnityEngine.EventSystems;

public class DartDetectOneTarget : MonoBehaviour
{
    [SerializeField]
    private string MyTargetObjTag = "Balloons";
    
    private Balloons CollisionBalloons; // 충돌된 풍선 오브젝트의 Balloons 스크립트를 참조하기 위한 변수

    void OnTriggerEnter(Collider other)
    {  
        if (string.IsNullOrEmpty(MyTargetObjTag)) return;

        Debug.Log($"Collision detected with {other.gameObject.name}");
        
        CollisionBalloons = other.gameObject.GetComponentInParent<Balloons>(); // 충돌된 오브젝트(hitBox 오브젝트)의 부모 오브젝트에서 Balloons 스크립트를 찾음
        
        if (other.CompareTag(MyTargetObjTag)) //타켓 오브젝트로 지정한 태그와 충돌된 오브젝트의 태그가 일치한다면 
        {
            MyTargetObjTag = string.Empty; // 힌반에 하나의 타켓오브젝만 상호작용을 하기 위해 타켓을 지움

            CollisionBalloons.HitBalloon();
        }
    }

}
