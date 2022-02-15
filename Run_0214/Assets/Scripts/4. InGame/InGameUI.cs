using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InGameUI : MonoBehaviour
{
    private static InGameUI instance;

    public Text scoreBoard;
    public GameObject startCountObject;
    private Text startCountText;

    public static InGameUI GetInstance()
    {
        if(instance == null)
        {
            Debug.LogError("InGameUI 인스턴스가 존재하지 않습니다.");
            return null;
        }

        return instance;
    }

    void Awake()
    {
        if (instance != null) Destroy(instance);
        instance = this;

        startCountText = startCountObject.GetComponentInChildren<Text>();
        startCountObject.SetActive(true);
    }

    public void SetScoreBoard(int score)
    {
        scoreBoard.text = "Remain Player : " + score;
    }

    public void SetStartCount(int time, bool isEnable = true)
    {
        startCountObject.SetActive(isEnable);
        if (isEnable)
        {
            if (time == 0)
                startCountText.text = "Game Start!";
            else
                startCountText.text = string.Format("{0}", time);
        }
    }
}
