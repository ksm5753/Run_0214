using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BackEnd;
using BackEnd.Tcp;

public partial class BackendMatchManager : MonoBehaviour
{
    private bool isSetHost = false;         //호스트 세션 결정했는지 여부
    //게임 로그
    private string FAIL_ACCESS_INGAME = "인게임 접속 실패 : {0} - {1}";
    private string SUCCESS_ACCESS_INGAME = "유저 인게임 접속 성공 : {0}";
    private string NUM_INGAME_SESSION = "인게임 내 세션 갯수 : {0}";

    //인게임 룸 접속
    private void AccessInGame(string roomToken)
    {
        Backend.Match.JoinGameRoom(roomToken);
    }

    //현재 룸에 접속한 세션들의 정보
    //최초 룸에 접속했을 때 1회 수신됨
    //재접속 했을 때도 1회 수신됨
    private void ProcessMatchInGameSessionList(MatchInGameSessionListEventArgs args)
    {
        sessionIdList = new List<SessionId>();
        gameRecords = new Dictionary<SessionId, MatchUserGameRecord>();

        foreach(var record in args.GameRecords)
        {
            sessionIdList.Add(record.m_sessionId);
            gameRecords.Add(record.m_sessionId, record);
        }

        sessionIdList.Sort();
    }

    //클라이언트 들의 게임 룸 접속에 대한 리턴값
    //클라이언트가 게임 룸에 접속할 때마다 호출됨
    //재접속 했을 때는 수신되지 않음
    private void ProcessMatchInGameAccess(MatchInGameSessionEventArgs args)
    {
        //if (isReconnectProcess) { }
        print("BackendInGame.cs - ProcessMatchInGameAccess - " + string.Format(SUCCESS_ACCESS_INGAME, args.ErrInfo));

        if(args.ErrInfo != ErrorCode.Success)
        {
            //게임 룸 접속 실패
            print("BackendInGame.cs - ProcessMatchInGameAccess - " + string.Format(FAIL_ACCESS_INGAME, args.ErrInfo, args.Reason));
            LeaveInGameRoom();
            return;
        }

        //게임 룸 접속 성공
        //인자값에 방금 접속한 클라이언트(세션)의 세션 ID와 매칭 기록이 적혀 있음
        //세션 정보는 누적되어 들어있기 때문에 이미 저장한 세션이면 건너뛴다.
        var record = args.GameRecord;
        print("BackendInGame.cs - ProcessMatchInGameAccess - " + string.Format(string.Format("인게임 접속 유저 정보 [{0}] : {1}", args.GameRecord.m_sessionId, args.GameRecord.m_nickname)));
        if (!sessionIdList.Contains(args.GameRecord.m_sessionId))
        {
            //세션 정보, 게임 기록 등을 저장
            sessionIdList.Add(record.m_sessionId);
            gameRecords.Add(record.m_sessionId, record);

            print("BackendInGame.cs - ProcessMatchInGameAccess - " + string.Format(NUM_INGAME_SESSION, sessionIdList.Count));
        }
    }

    //인게임 서버 접속 종료
    public void LeaveInGameRoom()
    {
        isConnectInGameServer = false;
        Backend.Match.LeaveGameServer();
    }

    //서버에서 게임 시작 패킷을 보냈을 때 호출
    //모든 세션이 게임 룸에 참여 후 "콘솔에서 설정한 시간" 후에 게임 시작 패킷이 서버에서 옴
    private void GameSetup()
    {
        print("BackendInGame.cs - GameSetup - 게임 시작 메시지 수신. 게임 설정 시작");

        isHost = false;
        isSetHost = false;

        OnGameReady();
    }

    //게임 레디 상태일 때 호출됨
    public void OnGameReady()
    {
        if (!isSetHost)
        {
            //호스트가 설정되어 있지 않으면 호스트 설정
            isSetHost = SetHostSession();
        }
        print("BackendInGame.cs - OnGameReady - 호스트 설정 완료");

        if(isSandBoxGame && IsHost())
        {
            SetAIPlayer();
        }

        if (IsHost())
        {
            Invoke("ReadyToLoadRoom", 0.5f);
        }
    }

    //샌드박스 모드 설정
    private void SetAIPlayer()
    {
        int aiCount = numOfClient - sessionIdList.Count;

        print("BackendInGame.cs - SetAIPlayer - AI 플레이어 설정 : aiCount : " + aiCount);

        int index = 0;
        for(int i = 0; i < aiCount; i++)
        {
            MatchUserGameRecord aiRecord = new MatchUserGameRecord();
            aiRecord.m_nickname = "AIPlayer" + index;
            aiRecord.m_sessionId = (SessionId)index;
            aiRecord.m_numberOfMatches = 0;
            aiRecord.m_numberOfWin = 0;
            aiRecord.m_numberOfDefeats = 0;
            aiRecord.m_numberOfDraw = 0;

            gameRecords.Add((SessionId)index, aiRecord);
            sessionIdList.Add((SessionId)index);
            index += 1;
        }
    }

    private void ReadyToLoadRoom()
    {
        if (BackendMatchManager.GetInstance().isSandBoxGame)
        {
            print("BackendInGame.cs - ReadyToLoadRoom - 샌드박스 모드 활성화. AI 정보 수신");
            //샌드박스 모드면 ai 정보 송신
            foreach (var tmp in gameRecords)
            {
                if ((int)tmp.Key > (int)SessionId.Reserve) continue;
                print("BackendInGame.cs - ReadyToLoadRoom - ai정보 송신 : " + (int)tmp.Key);
                SendDataToInGame(new Protocol.AIPlayerInfo(tmp.Value));
            }
        }

        print("BackendInGame.cs - ReadyToLoadRoom - 1초 후 룸 씬 전환 메시지 송신");
        Invoke("SendChangeRoomScene", 1f);
    }

    public bool PrevGameMessage(byte[] BinaryUserData)
    {
        Protocol.Message msg = DataParser.ReadJsonData<Protocol.Message>(BinaryUserData);
        if (msg == null) return false;

        //게임 설정 사전 작업 패킷 검사
        switch (msg.type)
        {
            case Protocol.Type.AIPlayerInfo:
                Protocol.AIPlayerInfo aiPlayerInfo = DataParser.ReadJsonData<Protocol.AIPlayerInfo>(BinaryUserData);
                ProcessAIData(aiPlayerInfo);
                return true;
            case Protocol.Type.LoadRoomScene:
                LobbyUI.GetInstance().ChangeRoomLoadScene(); //룸 대기실로 이동
                if (IsHost())
                {
                    print("BackendInGame.cs - PrevGameMessage - 5초후 게임 씬 전환 메시지 송신");
                    Invoke("SendChangeGameScene", 5);
                }
                return true;
            case Protocol.Type.LoadGameScene:
                GameManager.GetInstance().ChangeState(GameManager.GameState.Start); //게임 씬으로 전환
                return true;
        }
        return false;
    }

    public void ProcessAIData(Protocol.AIPlayerInfo aiPlayerInfo)
    {
        MatchInGameSessionEventArgs args = new MatchInGameSessionEventArgs();
        args.GameRecord = aiPlayerInfo.GetMatchRecord();

        ProcessMatchInGameAccess(args);
    }

    private void SendChangeRoomScene()
    {
        print("BackendInGame.cs - SendChangeRoomScene - 룸 씬 전환 메시지 송신");
        SendDataToInGame(new Protocol.LoadRoomSceneMessage());
    }

    private void SendChangeGameScene()
    {
        print("BackendInGame.cs - SendChangeGameScene - 게임 씬 전환 송신");
        SendDataToInGame(new Protocol.LoadGameSceneMessage());
    }

    //호스트에서 보낸 세션리스트로 갱신
     public void SetPlayerSessionList(List<SessionId> sessions)
    {
        sessionIdList = sessions;
    }

    //서버로 데이터 패킷 전송
    //서버에서는 이 패킷을 받아 모든 클라이언트(패킷 보낸 클라이언트 포함)로 브로드캐스팅 해줌
    public void SendDataToInGame<T>(T msg)
    {
        Backend.Match.SendDataToInGameRoom(DataParser.DataToJsonData<T>(msg));
    }

    private void ProcessSessionOffline(SessionId sessionId)
    {
        if (hostSession.Equals(sessionId))
        {
            // 호스트 연결 대기를 띄움
            //InGameUiManager.GetInstance().SetHostWaitBoard();
        }
        else
        {
            // 호스트가 아니면 단순히 UI 만 띄운다.
        }
    }

    private void SendGameSyncMessage()
    {
        //현재 게임 상황
        //var message = 
        //SendDataToInGame();
    }
}
