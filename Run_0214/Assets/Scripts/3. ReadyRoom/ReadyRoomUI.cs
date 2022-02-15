using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BackEnd;
using BackEnd.Tcp;
using System.Linq;

public class ReadyRoomUI : MonoBehaviour
{
    private static ReadyRoomUI instance;   // 인스턴스

    public GameObject[] userObject = new GameObject[4];

    private int numOfClient = 0;
    private const string WIN_RECORD = "승 : {0}";
    private const string DEFEAT_RECORD = "패 : {0}";

    void Start()
    {
        var matchInstance = BackendMatchManager.GetInstance();
        if (matchInstance == null) return;

        numOfClient = matchInstance.gameRecords.Count;

        byte index = 0;
        foreach(var record in matchInstance.gameRecords.OrderByDescending(x => x.Key))
        {
            var name = record.Value.m_nickname;
            string score = string.Empty;

            var data = userObject[index++].GetComponentsInChildren<Text>();
            data[0].text = name;
            data[1].text = string.Format(WIN_RECORD, record.Value.m_numberOfWin);
            data[2].text = string.Format(DEFEAT_RECORD, record.Value.m_numberOfDefeats);
        }
    }

    void Awake()
    {
        if (instance != null)
        {
            Destroy(instance);
        }
        instance = this;
    }

    public static ReadyRoomUI GetInstance()
    {
        if (instance == null)
        {
            Debug.LogError("BackendServerManager 인스턴스가 존재하지 않습니다.");
            return null;
        }

        return instance;
    }
}
