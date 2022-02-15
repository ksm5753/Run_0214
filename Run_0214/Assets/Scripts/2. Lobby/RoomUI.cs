using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BackEnd;
using BackEnd.Tcp;
using Battlehub.Dispatcher;
using System.Linq;

public partial class LobbyUI : MonoBehaviour
{
    public void MakeRoom()
    {
        //매치 서버에 대기방 생성 요청
        if (BackendMatchManager.GetInstance().CreateMatchRoom())
        {
            //SetLoadingObjectActive(true);
        }
    }

    public void RoomResult(bool isSuccess, List<MatchMakingUserInfo> userList = null)
    {
        //대기방 생성에 성공시
        if (isSuccess)
        {
            print("RoomUI - RoomResult - 대기방 생성 성공");

            RequestMatch();
        }
    }
    public void CancelMatch()
    {
        if (loadingObject.activeSelf || errorObject.activeSelf || matchDonePanel.activeSelf)
            return;

        BackendMatchManager.GetInstance().CancelRequestMatchMaking();
    }

    public void RequestMatch()
    {
        if (loadingObject.activeSelf || errorObject.activeSelf || matchDonePanel.activeSelf || matchRequestPanel.activeSelf)
            return;

        BackendMatchManager.GetInstance().RequestMatchMaking(0);
    }

    public void MatchDoneCallback()
    {
        matchRequestPanel.SetActive(false);
        matchDonePanel.SetActive(true);
    }

    public void MatchCancelCallback()
    {
        matchRequestPanel.SetActive(true);
        matchDonePanel.SetActive(false);
    }

    public void MatchRequestCallback(bool result)
    {
        matchRequestPanel.SetActive(result);
    }
}
