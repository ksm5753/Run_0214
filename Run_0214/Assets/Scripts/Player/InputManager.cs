using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Protocol;

public class InputManager : MonoBehaviour
{
    private bool isRotate = false;

    public Movement_Joystick virtualStick;

    void Start()
    {
        GameManager.InGame += MobileInput; //회전
        GameManager.AfterInGame += SendNoMoveMessage;
    }

    void PlayerMove()
    {

    }

    void MobileInput()
    {
        if (!virtualStick) return;

        int keyCode = 0;
        isRotate = false;

        print("누름");

        //if (!virtualStick.isInputEnable)
        //{
        //    isRotate = false;

        //    return;
        //}

        if(virtualStick.isInputEnable)
            isRotate = true;

        keyCode |= (isRotate ? KeyEventCode.ROTATE : KeyEventCode.NO_ROTATE);

        if (keyCode <= 0)
        {
            print("안움직임");
            return;
        }

        KeyMessage msg = new KeyMessage(keyCode, virtualStick.joystickVec.x);


        if (BackendMatchManager.GetInstance().IsHost())
        {
            print("보냄");
            BackendMatchManager.GetInstance().AddMsgToLocalQueue(msg);
        }
        else
        {
            print("엥?");
            BackendMatchManager.GetInstance().SendDataToInGame<KeyMessage>(msg);
        }


    }

    void SendNoMoveMessage()
    {
        int keyCode = 0;

        if(!isRotate && WorldManager.instance.IsMyPlayerRotate())
        {
            keyCode |= KeyEventCode.NO_ROTATE;
        }
        if (keyCode == 0) return;

        KeyMessage msg = new KeyMessage(keyCode, 0);

        if (BackendMatchManager.GetInstance().IsHost())
            BackendMatchManager.GetInstance().AddMsgToLocalQueue(msg);
        else
            BackendMatchManager.GetInstance().SendDataToInGame<KeyMessage>(msg);
    }
}
