using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Protocol;
using BackEnd;
using BackEnd.Tcp;

public class WorldManager : MonoBehaviour
{
    public static WorldManager instance;
    private const int START_COUNT = 3;

    private SessionId myPlayerIndex = SessionId.None;

    #region Player
    public GameObject playerPool;           //�÷��̾� ������
    public GameObject playerPrefab;         //�÷��̾� ������
    public int numOfPlayer = 0;
    private const int MAX_PLAYER = 4;
    public int alivePlayer { get; set; }
    private Dictionary<SessionId, Player> players;
    public GameObject startPointObject;
    private List<Vector3> startingPoints;

    private Stack<SessionId> gameRecord;
    public delegate void PlayerDie(SessionId index);
    public PlayerDie dieEvent;

    #endregion

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        InitializeGame();

        var matchInstance = BackendMatchManager.GetInstance();
        if (matchInstance == null) return;
        if (matchInstance.isReconnectProcess)
        {
            //InGameUI.GetInstance().SetStartCount(0, false);
            //InGameUI.GetInstance()
        }
    }

    /*
     * �÷��̾� ����
     * ���� ���� �Լ� ����
     */
    public bool InitializeGame()
    {
        if (!playerPool)
        {
            print("WorldManager.cs - InitializeGame - Player Pool Not Exist!");
            return false;
        }
        print("WorldManager.cs - InitializeGame - ���� �ʱ�ȭ ����");
        gameRecord = new Stack<SessionId>();
        //GameManager.


        myPlayerIndex = SessionId.None;
        SetPlayerAttribute(); //�÷��̾� ��ġ
        GameStart();
        return true;
    }

    public void SetPlayerAttribute()
    {
        //������
        startingPoints = new List<Vector3>();

        int num = startPointObject.transform.childCount;
        for(int i = 0; i < num; i++)
        {
            var child = startPointObject.transform.GetChild(i);
            Vector3 point = child.transform.position;
            startingPoints.Add(point);
        }

        dieEvent += PlayerDieEvent;
    }

    private void PlayerDieEvent(SessionId index)
    {
        alivePlayer -= 1;
        players[index].gameObject.SetActive(false);

        InGameUI.GetInstance().SetScoreBoard(alivePlayer);
        gameRecord.Push(index);

        print("WorldManager.cs - PlayerDieEvent - " + string.Format("Player Die : " + players[index].GetNickName()));

        //ȣ��Ʈ�� �ƴϸ� �ٷ� ����
        if (!BackendMatchManager.GetInstance().IsHost()) return;

        //1�� ���Ϸ� �÷��̾ ������ �ٷ� ���� üũ
        if (alivePlayer <= 1)
            SendGameEndOrder();
    }

    private void SendGameEndOrder()
    {
        //���� ���� ��ȯ �޽����� ȣ��Ʈ������ ����
        print("WorldManager.cs - SendGameEndOrder - Make GameResult & Send Game End Order");
        foreach(SessionId session in BackendMatchManager.GetInstance().sessionIdList)
        {
            if (players[session].GetIsLive() && !gameRecord.Contains(session))
                gameRecord.Push(session);
        }

        GameEndMessage message = new GameEndMessage(gameRecord);
        BackendMatchManager.GetInstance().SendDataToInGame<GameEndMessage>(message);
    }

    public void GameStart()
    {
        if(BackendMatchManager.GetInstance() == null)
        {
            return;
        }

        if (BackendMatchManager.GetInstance().IsHost())
        {
            print("WorldManager.cs - GameStart - �÷��̾� �������� Ȯ��");

            if (BackendMatchManager.GetInstance().IsSessionListNull())
            {
                print("WorldManaer.cs - GameStart - Player Index Not Exist!");
                //ȣ��Ʈ ���� ���ǵ����Ͱ� ������ ������ �ٷ� �����Ѵ�.
                foreach(var session in BackendMatchManager.GetInstance().sessionIdList)
                {
                    //���� ������� ���ÿ� �߰�
                    gameRecord.Push(session);
                }

                GameEndMessage gameEndMessage = new GameEndMessage(gameRecord);
                BackendMatchManager.GetInstance().SendDataToInGame<GameEndMessage>(gameEndMessage);
                return;
            }
        }
        SetPlayerInfo();
    }

    public void SetPlayerInfo()
    {
        if(BackendMatchManager.GetInstance().sessionIdList == null)
        {
            //���� ���� ID����Ʈ�� �������� ������, 0.5�� �� �ٽ� ����
            Invoke("SetPlayerInfo", 0.5f);
            return;
        }

        var gamers = BackendMatchManager.GetInstance().sessionIdList;
        int size = gamers.Count;
        if(size <= 0)
        {
            print("WorldManager.cs - SetPlayerInfo - No Player Exist!");
            return;
        }
        if(size > MAX_PLAYER)
        {
            print("WorldManager.cs - SetPlayerInfo -   Player Pool Exceed");
            return;
        }

        players = new Dictionary<SessionId, Player>();
        BackendMatchManager.GetInstance().SetPlayerSessionList(gamers);

        int index = 0;
        foreach(var sessionId in gamers)
        {
            GameObject player = Instantiate(playerPrefab, new Vector3(startingPoints[index].x, startingPoints[index].y, startingPoints[index].z), Quaternion.identity, playerPool.transform);
            players.Add(sessionId, player.GetComponent<Player>());

            if (BackendMatchManager.GetInstance().IsMySessionId(sessionId))
            {
                myPlayerIndex = sessionId;
                players[sessionId].Initialize(true, myPlayerIndex, BackendMatchManager.GetInstance().GetNickNameBySessionId(sessionId));
            }
            else
            {
                players[sessionId].Initialize(false, sessionId, BackendMatchManager.GetInstance().GetNickNameBySessionId(sessionId));
            }
            index += 1;
        }
        print("WorldManager.cs - SetPlayerInfo - Num Of Current Player : " + size);

        //���ھ� ���� ����
        alivePlayer = size;
        InGameUI.GetInstance().SetScoreBoard(alivePlayer);

        if (BackendMatchManager.GetInstance().IsHost())
            StartCoroutine("StartCount");
    }

    IEnumerator StartCount()
    {
        StartCountMessage msg = new StartCountMessage(START_COUNT);

        //ī��Ʈ �ٿ�
        for(int i = 0; i < START_COUNT + 1; i++)
        {
            msg.time = START_COUNT - i;
            BackendMatchManager.GetInstance().SendDataToInGame<StartCountMessage>(msg);
            yield return new WaitForSeconds(1);
        }

        //���� ���� �޽��� ����
        GameStartMessage gameStartMessage = new GameStartMessage();
        BackendMatchManager.GetInstance().SendDataToInGame<GameStartMessage>(gameStartMessage);
    }

    public void OnRecieve(MatchRelayEventArgs args)
    {
        if(args.BinaryUserData == null)
        {
            print("WorldManager.cs - OnRecieve - " + string.Format("�� �����Ͱ� ��ε�ĳ���� �Ǿ����ϴ�.\n{0} - {1}", args.From, args.ErrInfo));
            //�����Ͱ� ������ �׳� ����
            return;
        }

        Message msg = DataParser.ReadJsonData<Message>(args.BinaryUserData);
        if (msg == null) return;

        if (BackendMatchManager.GetInstance().IsHost() != true && args.From.SessionId == myPlayerIndex) return;

        if(players == null)
        {
            print("WorldManager.cs - OnRecieve - Players ������ �������� �ʽ��ϴ�.");
            return;
        }

        switch (msg.type)
        {
            case Type.StartCount:
                StartCountMessage startCount = DataParser.ReadJsonData<StartCountMessage>(args.BinaryUserData);
                print("WorldManager.cs - OnRecieve - Wait Second : " + startCount.time);
                InGameUI.GetInstance().SetStartCount(startCount.time);
                break;
            case Type.GameStart:
                InGameUI.GetInstance().SetStartCount(0, false);
                GameManager.GetInstance().ChangeState(GameManager.GameState.InGame);
                break;

            case Type.Key:
                KeyMessage keyMessage = DataParser.ReadJsonData<KeyMessage>(args.BinaryUserData);
                ProcessKeyEvent(args.From.SessionId, keyMessage);
                break;
            case Type.PlayerRotate:
                PlayerRotateMessage rotateMessage = DataParser.ReadJsonData<PlayerRotateMessage>(args.BinaryUserData);
                ProcessPlayerData(rotateMessage);
                break;
            case Type.PlayerNoRotate:
                PlayerNoRotateMessage noRotateMessage = DataParser.ReadJsonData<PlayerNoRotateMessage>(args.BinaryUserData);
                ProcessPlayerData(noRotateMessage);
                break;
        }
    }

    public void OnRecieveForLocal(KeyMessage keyMessage)
    {
        ProcessKeyEvent(myPlayerIndex, keyMessage);
    }

    public void OnRecieveForLocal(PlayerNoRotateMessage message)
    {
        ProcessPlayerData(message);
    }

    //���� �ۿ�
    private void ProcessKeyEvent(SessionId index, KeyMessage keyMessage)
    {
        if (!BackendMatchManager.GetInstance().IsHost()) return;


        int keyData = keyMessage.keyData;

        Vector3 moveVector = Vector3.zero;
        Vector3 playerPos = players[index].GetPosition();


        //if((keyData & KeyEventCode.MOVE) == KeyEventCode.MOVE)
        //{
        //    print("������!!!");
        //    moveVector = new Vector3(1, 0, 0);
        //    players[index].SetMoveVector(moveVector);
        //    PlayerMoveMessage msg = new PlayerMoveMessage(index, playerPos, moveVector);
        //    BackendMatchManager.GetInstance().SendDataToInGame<PlayerMoveMessage>(msg);
        //}

        moveVector = new Vector3(keyMessage.x, 0, 0);

        if((keyData & KeyEventCode.ROTATE) == KeyEventCode.ROTATE){
            print("ROTATE ON");

            players[index].SetMoveVector(moveVector);
            PlayerRotateMessage msg = new PlayerRotateMessage(index, playerPos, moveVector);
            BackendMatchManager.GetInstance().SendDataToInGame<PlayerRotateMessage>(msg);
        }
        if((keyData & KeyEventCode.NO_ROTATE) == KeyEventCode.NO_ROTATE)
        {
            print("Rotate OFF");
            players[index].SetMoveVector(moveVector);
            PlayerNoRotateMessage msg = new PlayerNoRotateMessage(index, playerPos, new Vector3(0, 0, 0));
            BackendMatchManager.GetInstance().SendDataToInGame<PlayerNoRotateMessage>(msg);
        }

    }


    //��Ƽ �����鿡�� �޽��� ����
    //private void ProcessPlayerData(PlayerMoveMessage data)
    //{
    //    if (BackendMatchManager.GetInstance().IsHost()) return;

    //    Vector3 moveVector = new Vector3(data.xDir, data.yDir, data.zDir);
    //    if (!moveVector.Equals(players[data.playerSession].moveVector))
    //    {
    //        print("AAA");
    //        players[data.playerSession].SetPosition(data.xPos, data.yPos, data.zPos);
    //        players[data.playerSession].SetMoveVector(moveVector);
    //    }
    //}
    private void ProcessPlayerData(PlayerRotateMessage data)
    {

        if (BackendMatchManager.GetInstance().IsHost()) return;
        print("�湮 on");

        Vector3 moveVector = new Vector3(data.xDir, data.yDir, data.zDir);
        //if (!moveVector.Equals(players[data.playerSession].moveVector))
        //{
            players[data.playerSession].SetPosition(data.xPos, data.yPos, data.zPos);
            players[data.playerSession].SetMoveVector(moveVector);
        
            //players[data.playerSession].SetRotate(data.x);
    }

    private void ProcessPlayerData(PlayerNoRotateMessage data)
    {
        if (BackendMatchManager.GetInstance().IsHost()) return;
        print("�湮 off");


        Vector3 moveVector = new Vector3(data.xDir, data.yDir, data.zDir);

            players[data.playerSession].SetPosition(data.xPos, data.yPos, data.zPos);
            players[data.playerSession].SetMoveVector(moveVector);
        
        //players[data.playerSession].SetRotate(data.x);
    }

    public bool IsMyPlayerRotate()
    {
        return players[myPlayerIndex].isRotate;
    }
}
