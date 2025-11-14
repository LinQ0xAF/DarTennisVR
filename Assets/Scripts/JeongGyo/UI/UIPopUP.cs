using UnityEngine;
using System;
public class UIPopUp : MonoBehaviour
{
   public event Action<UIPopUp> OnHit;


    public void HitUI() 
    {
        OnHit?.Invoke(this);
        Debug.Log($"[UI]:{this.name} [Active]:HitUIButton invoked");

    }

    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.layer == LayerMask.NameToLayer("Darts"))
        {
            HitUI();
        }
    }


} 


