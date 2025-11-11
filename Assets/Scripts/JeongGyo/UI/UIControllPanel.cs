using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UIElements;


public class UIControllPanel : MonoBehaviour
{
    [SerializeField]
    private GameObject SingeleUIPanelObj;
    
    [SerializeField]
    private GameObject MultiUIPanelObj;

    [SerializeField]
    private bool isSingleUI = true;

    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Dart"))
        {
            if (isSingleUI)
            {
                SingeleUIPanelObj.SetActive(true);
                MultiUIPanelObj.SetActive(false);
            }
            else
            {
                SingeleUIPanelObj.SetActive(false);
                MultiUIPanelObj.SetActive(true);
            }
            
        }
    }


}
