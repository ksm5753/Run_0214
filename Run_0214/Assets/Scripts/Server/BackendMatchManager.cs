using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BackEnd;
using BackEnd.Tcp;
using Battlehub.Dispatcher;
using System.Linq;
using LitJson;
using System;
using Protocol;

/*
 * ##### BackendMatchManager.cs에서 사용하는 기능
 * 매치메이킹 핸들러 등록
 * 인게임 핸들러 등록
 */



public partial class BackendMatchManager : MonoBehaviour
{
    private static BackendMatchManager instance;

    //콘솔에서 생성한 매칭 카드 정보
    public class MatchInfo
    {
        public string title;                //매칭 명
        public string inDate;               //매칭 inDate (UUID)
        public MatchType matchType;         //매치 타입 (1:1 / 개인전 / 팀전)
        public MatchModeType matchModeType; //매치 모드 타입 (랜덤 / 포인트 / MMR)
        public string headCount;            //매칭 인원
        public bool isSandBoxEnable;        //샌드박스 모드 (AI 매칭)
    }
    public List<MatchInfo> matchInfos { get; private set; } = new List<MatchInfo>(); //콘솔에서 생성한 매칭 테이블 리스트
    public List<SessionId> sessionIdList { get; private set; } //매치에 참가중인 유저들의 세션 목록

    public Dictionary<SessionId, MatchUserGameRecord> gameRecords { get; private set; } = null;  // 매치에 참가중인 유저들의 매칭 기록

    private string inGameRoomToken = string.Empty; //게임 룸 토큰 (인게임 접속 토큰)

    public SessionId hostSession { get; private set; } //호스트 세션

    public bool isReconnectEnable { get; private set; } = false;

    public bool isConnectMatchServer { get; private set; } = false;
    private bool isConnectInGameServer = false;
    private bool isJoinGameRoom = false;
    public bool isReconnectProcess { get; private set; } = false;
    public bool isSandBoxGame { get; private set; } = false;

    //HOST
    private bool isHost = false;            //호스트 여부 (서버에서 설정한 SuperGamer 정보를 가져옴
    private Queue<KeyMessage> localQueue = null;

    private int numOfClient = 2;            //매치에 참가한 유저의 총 수
    //private Queue<K>
    //private Queue<K>

    void Awake()
    {
        if (instance != null)
            Destroy(instance);
        instance = this;
    }

    public static BackendMatchManager GetInstance()
    {
        if (!instance)
            return null;
        return instance;
    }

    void Start()
    {
        MatchMakingHandler();
        InGameHandler();
    }

    void OnApplicationQuit()
    {
        if (isConnectMatchServer)
            LeaveMatchServer();
    }

    //호스트인지 확인
    public bool IsHost()
    {
        return isHost;
    }

    public void SetHostSession(SessionId host)
    {
        hostSession = host;
    }

    //세션 아이디 확인
    public bool IsMySessionId(SessionId session)
    {
        return Backend.Match.GetMySessionId() == session;
    }

    public string GetNickNameBySessionId(SessionId session)
    {
        // return Backend.Match.GetNickNameBySessionId(session);
        return gameRecords[session].m_nickname;
    }

    public bool IsSessionListNull()
    {
        return sessionIdList == null || sessionIdList.Count == 0;
    }

    public void AddMsgToLocalQueue(KeyMessage message)
    {
        //로컬 큐에 메시지 추가
        if (!isHost || localQueue == null) return;

        localQueue.Enqueue(message);
    }

    private bool SetHostSession()
    {
        //호스트 세션 정하기
        //각 클라이언트가 모두 수행 (호스트 세션 정하는 로직은 모두 같으므로 각각의 클라이언트가 모두 로직을 수행하지만 결과값은 동일)
        print("BackendMatchManager.cs - SetHostSession - 호스트 세션 설정 진입");
        //호스트 세션 정렬(각 클라이언트마다 입장 순서가 다를 수 있기 때문에 정렬)
        sessionIdList.Sort();
        isHost = false;

        //내가 호스트 세션인지 확인
        foreach(var record in gameRecords)
        {
            if (record.Value.m_isSuperGamer)
            {
                if (record.Value.m_sessionId.Equals(Backend.Match.GetMySessionId()))
                    isHost = true;
                hostSession = record.Value.m_sessionId; //해당 유저를 호스트로 설정
                break;
            }
        }

        print("BackendMatchManager.cs - SetHostSession - 호스트 여부 : " + isHost);

        // 호스트 세션이면 로컬에서 처리하는 패킷이 있으므로 로컬 큐를 생성해준다
        if (isHost)
        {
            localQueue = new Queue<KeyMessage>();
        }
        else
        {
            localQueue = null;
        }

        //호스트 설정까지 끝나면 매치서버와 접속 끊음
        LeaveMatchServer();
        return true;
    }

    private void SetSubHost(SessionId hostSessionId)
    {
        print("BackendMatchManager.cs - SetSubHost - 서브 호스트 세션 설정 진입");

        //누가 서브 호스트 세션인지 서버에서 보낸 정보값 확인
        //서버에서 보낸 SuperGamer 정보로 GameRecords의 SuperGamer 정보 갱신
        foreach(var record in gameRecords)
        {
            if (record.Value.m_sessionId.Equals(hostSessionId))
                record.Value.m_isSuperGamer = true;
            else
                record.Value.m_isSuperGamer = false;
        }

        //내가 호스트 세션인지 확인
        if (hostSessionId.Equals(Backend.Match.GetMySessionId()))
            isHost = true;
        else
            isHost = false;

        hostSession = hostSessionId; //주어진 세션을 호스트 세션으로 설정

        print("BackendMatchManager.cs - SetSubHost - 서브 호스트 여부 : " + isHost);

        //호스트 세션이면 로컬
        //ㅇㄹㅇㅁㄹㅇㅁㄹ

        print("BackendMatchManager.cs - SetSubHost - 서브 호스트 설정 완료");
    }

    //매칭 서버 관련 이벤트 핸들러
    private void MatchMakingHandler()
    {
        Backend.Match.OnJoinMatchMakingServer += (args) =>
        {
            print("BackendMatchManager.cs - MatchMakingHandler - 매칭 서버 접속 : " + args.ErrInfo);
            ProcessAccessMatchMakingServer(args.ErrInfo);
        };

        Backend.Match.OnMatchMakingResponse += (args) =>
        {
            print("BackendMatchManager.cs - MatchMakingHandler - 매칭 신청 : " + args.ErrInfo + " : " + args.Reason);
            ProcessMatchMakingResponse(args);
        };

        Backend.Match.OnLeaveMatchMakingServer += (args) =>
        {
            print("BackendMatchManager.cs - MatchMakingHandler - 매칭 서버 종료 : " + args.ErrInfo);
            isConnectMatchServer = false;

            //다시 재접속 시도
            JoinMatchServer();

            if (args.ErrInfo.Category.Equals(ErrorCode.DisconnectFromRemote) || args.ErrInfo.Category.Equals(ErrorCode.Exception)
                || args.ErrInfo.Category.Equals(ErrorCode.NetworkTimeout))
            {
                // 서버에서 강제로 끊은 경우
                //if (LobbyUI.GetInstance())
                //{
                //    LobbyUI.GetInstance().MatchRequestCallback(false);
                //    LobbyUI.GetInstance().CloseRoomUIOnly();
                //    LobbyUI.GetInstance().SetErrorObject("매칭서버와 연결이 끊어졌습니다.\n\n" + args.ErrInfo.Reason);
                //}
            }
        };

        //대기 방 생성 / 실패 여부
        Backend.Match.OnMatchMakingRoomCreate += (args) =>
        {
            print("BackendMatchManager.cs - MatchMakingHandler - 대기 방 생성 : " + args.ErrInfo);

            LobbyUI.GetInstance().RoomResult(args.ErrInfo.Equals(ErrorCode.Success) == true);
        };
    }

    //인게임 서버 관련 이벤트 핸들러
    private void InGameHandler()
    {
        Backend.Match.OnSessionJoinInServer += (args) =>
        {
            print("BackendMatchManager.cs - InGameHandler - 인게임 서버 접속 : " + args.ErrInfo);

            if (isJoinGameRoom) return;
            if (inGameRoomToken == string.Empty)
            {
                print("인게임 서버 접속 성공했으나 룸 토큰이 존재하지 않습니다.");
                return;
            }

            print("BackendMathManager.cs - InGameHandler - 인게임 서버 접속 성공!");
            isJoinGameRoom = true;
            AccessInGame(inGameRoomToken);

        };

        Backend.Match.OnSessionListInServer += (args) =>
        {
            //세션 리스트 호출 후 조인 채널이 호출됨
            //현재 같은 게임(방)에 참가중인 플레이어들 중 나보다 먼저 이 방에 들어와 있는 플레이어들과 나의 정보가 들어있음
            //나보다 늦게 들어온 플레이어들의 정보는 OnMatchInGameAccess에서 수신됨
            print("BackendMatchManager.cs - InGameHandler - OnSessionListInServer : " + args.ErrInfo);

            ProcessMatchInGameSessionList(args);
        };

        Backend.Match.OnMatchInGameAccess += (args) =>
        {
            print("BackendMatchManager.cs - InGameHandler - OnMatchInGameAccess : " + args.ErrInfo);

            ProcessMatchInGameAccess(args);
        };

        Backend.Match.OnMatchInGameStart += () =>
        {
            //서버에서 게임 시작 패킷을 보내면 호출
            GameSetup();
        };

        Backend.Match.OnMatchRelay += (args) =>
        {
            // 각 클라이언트들이 서버를 통해 주고받은 패킷들
            // 서버는 단순 브로드캐스팅만 지원 (서버에서 어떠한 연산도 수행하지 않음)

            //게임 사전 설정
            if (PrevGameMessage(args.BinaryUserData))
            {
                //게임 사전 설정을 진행하였으면 바로 리턴
                return;
            }

            if (WorldManager.instance == null) return;

            WorldManager.instance.OnRecieve(args);
        };

        Backend.Match.OnLeaveInGameServer += (args) =>
        {
            print("BackendMatchManager.cs - InGameHandler - OnLeaveInGameServer : " + args.ErrInfo + " : " + args.Reason);
            if (args.Reason.Equals("Fail To Reconnect"))
            {
                JoinMatchServer();
            }
            isConnectInGameServer = false;
        };

        Backend.Match.OnSessionOffline += (args) =>
        {
            //다른 유저 혹은 자기자신이 접속이 끊어졌을 때 호출
            print("BackendMatchManager.cs - InGameHandler - " + string.Format("[{0}] 오프라인되었습니다. - {1} : {2}", args.GameRecord.m_nickname, args.ErrInfo, args.Reason));
            //인증 오류가 아니면 오프라인 프로세스 실행
            if (args.ErrInfo != ErrorCode.AuthenticationFailed)
            {
                ProcessSessionOffline(args.GameRecord.m_sessionId);
            }
            else
            {
                // 잘못된 재접속 시도 시 인증오류가 발생
            }
        };

        Backend.Match.OnMatchResult += (args) =>
        {
            print("BackendMatchManager.cs - InGameHandler - 게임 결과값 업로드 결과 : " + string.Format("{0} : {1}", args.ErrInfo, args.Reason));

            //서버에서 게임 결과 패킷을 보내면 호출
            //내가(클라이언트) 서버로 보낸 결과값이 정상적으로 업데이트 되었는지 확인

            if (args.ErrInfo == ErrorCode.Success)
            {

            }
        };

        Backend.Match.OnChangeSuperGamer += (args) =>
        {
            print("BackendMatchManager.cs - InGameHandler - " + string.Format("이전 방장 : {0} / 새 방장 : {1}", args.OldSuperUserRecord.m_nickname, args.NewSuperUserRecord.m_nickname));
            //호스트 재설정
            SetSubHost(args.NewSuperUserRecord.m_sessionId);
            if (isHost)
            {
                //만약 서브 호스트로 설정되면 다른 모든 클라이언트에 싱크메시지 전송
                Invoke("SendGameSyncMessage", 1);
            }
        };
    }


    public void GetMatchList(Action<bool, string> func)
    {
        //매칭 테이블 정보 초기화
        matchInfos.Clear();

        Backend.Match.GetMatchList(callback =>
        {
            //요청 실패하는 경우 재요청
            if (!callback.IsSuccess())
            {
                print("BackendMatchManager.cs - GetMatchList - 매칭카드 리스트 불러오기 실패\n" + callback);
                Dispatcher.Current.BeginInvoke(() =>
                {
                    GetMatchList(func);
                });
                return;
            }

            foreach(JsonData row in callback.Rows())
            {
                MatchInfo matchInfo = new MatchInfo();
                matchInfo.title = row["matchTitle"]["S"].ToString();
                matchInfo.inDate = row["inDate"]["S"].ToString();
                matchInfo.headCount = row["matchHeadCount"]["N"].ToString();
                matchInfo.isSandBoxEnable = row["enable_sandbox"]["BOOL"].ToString().Equals("True") ? true : false;

                foreach(MatchType type in Enum.GetValues(typeof(MatchType)))
                {
                    if (type.ToString().ToLower().Equals(row["matchType"]["S"].ToString().ToLower()))
                        matchInfo.matchType = type;
                }

                foreach (MatchModeType type in Enum.GetValues(typeof(MatchModeType)))
                {
                    if (type.ToString().ToLower().Equals(row["matchModeType"]["S"].ToString().ToLower()))
                    {
                        matchInfo.matchModeType = type;
                    }
                }

                matchInfos.Add(matchInfo);
            }

            print("BackendMatchManager.cs - GetMatchList - 테이블리스트 불러오기 성공 : " + matchInfos.Count);
            func(true, string.Empty);
        });
    }

    public MatchInfo GetMatchInfo(string indate)
    {
        var result = matchInfos.FirstOrDefault(x => x.inDate == indate);
        if (result.Equals(default(MatchInfo)) == true)
        {
            return null;
        }
        return result;
    }

    public void IsMatchGameActivate()
    {
        isReconnectEnable = false;
        JoinMatchServer();
    }


    void Update()
    {
        if(isConnectInGameServer || isConnectMatchServer)
        {
            Backend.Match.Poll();

            //호스트의 경우 로컬큐가 존재
            //큐에 있는 패킷을 로컬에서 처리
            if(localQueue != null)
            {
                while(localQueue.Count > 0)
                {
                    var msg = localQueue.Dequeue();
                    WorldManager.instance.OnRecieveForLocal(msg);
                }
            }
        }
    }
}
