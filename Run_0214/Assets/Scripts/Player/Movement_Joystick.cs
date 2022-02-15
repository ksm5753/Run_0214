using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Movement_Joystick : MonoBehaviour
{
    public GameObject joystick;
    public GameObject joystickBg;
    public Vector2 joystickVec;
    private Vector2 joystickTouchPos;
    private Vector2 joystickOriginalPos;
    private float joystickRadius;

    public bool isInputEnable { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        isInputEnable = false;
        joystickOriginalPos = joystickBg.transform.position;
        joystickRadius = joystickBg.GetComponent<RectTransform>().sizeDelta.y / 4;
    }

    public virtual void PointerDown()
    {
        joystick.gameObject.SetActive(true);
        joystickBg.gameObject.SetActive(true);

        joystick.transform.position = Input.mousePosition;
        joystickBg.transform.position = Input.mousePosition;
        joystickTouchPos = Input.mousePosition;
    }

    public virtual void Drag(BaseEventData baseEventData)
    {
        PointerEventData pointerEventData = baseEventData as PointerEventData;
        Vector2 dragPos = pointerEventData.position;
        joystickVec = (dragPos - joystickTouchPos).normalized;

        float joystickDist = Vector2.Distance(dragPos, joystickTouchPos);

        if (joystickDist < joystickRadius)
        {
            joystick.transform.position = joystickTouchPos + joystickVec * joystickDist;
        }
        else
        {
            joystick.transform.position = joystickTouchPos + joystickVec * joystickRadius;
        }

        isInputEnable = true;
    }

    public virtual void PointerUp()
    {
        joystick.gameObject.SetActive(false);
        joystickBg.gameObject.SetActive(false);

        joystickVec = Vector2.zero;
        joystick.transform.position = joystickOriginalPos;
        joystickBg.transform.position = joystickOriginalPos;

        isInputEnable = false;
    }
}
