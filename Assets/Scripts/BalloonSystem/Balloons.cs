using UnityEngine;
using System;
public class Balloons : MonoBehaviour
{
   public event Action<Balloons> OnHit;

   // 외부에서 이 메서드를 호출하면 이벤트가 발생
   public void HitBalloon() // 풍선이 맞았을 때 다트호출되는 메서드
   {
      // 구독자가 있으면 this(어떤 풍선인지)를 전달하며 호출
      OnHit?.Invoke(this);
      Debug.Log($"[BalloonObj]:{this.name} [Active]:HitBalloon invoked");
        
      //풍선 객체가 터질때 실행할 이펙트나 사운드가 있으면 여기에 추가
   }
 
 } 


