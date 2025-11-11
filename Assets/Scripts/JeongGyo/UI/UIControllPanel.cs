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
    private GameObject _MainUIPanelObj;

    [SerializeField]
    private bool isSingleUI = true;

    private int _DartsLayerIndex;

    void Awake()
    {
        _DartsLayerIndex = LayerMask.NameToLayer("Darts");
    }


    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == _DartsLayerIndex)
        {
            Debug.Log("Dart hit UI Control Panel");

            if (isSingleUI)
            {
                SingeleUIPanelObj.SetActive(true);
                MultiUIPanelObj.SetActive(false);
                _MainUIPanelObj.SetActive(false);
            }
            else
            {
                SingeleUIPanelObj.SetActive(false);
                MultiUIPanelObj.SetActive(true);
                _MainUIPanelObj.SetActive(false);
            }
            
        }
    }


}
