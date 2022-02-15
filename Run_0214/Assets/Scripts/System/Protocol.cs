using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BackEnd.Tcp;

namespace Protocol
{
    //이벤트 타입
    public enum Type : sbyte
    {
        AIPlayerInfo,


        Key,

        PlayerRotate,
        PlayerNoRotate,

        StartCount,
        LoadRoomScene,
        LoadGameScene,
        GameSync,
        GameStart,
        GameEnd
    }

    //조이스틱 키 이벤트 코드
    public static class KeyEventCode
    {
        public const int MOVE = 0;
        public const int ROTATE = 1; //회전 메시지
        public const int NO_ROTATE = 2; //회전 멈춤 메시지
    }

    public class Message
    {
        public Type type;

        public Message(Type type)
        {
            this.type = type;
        }
    }

    public class KeyMessage : Message
    {
        public int keyData;
        public float x;

        public KeyMessage(int data, float x) : base(Type.Key)
        {
            this.keyData = data;
            this.x = x;
        }
    }


    public class AIPlayerInfo : Message
    {
        public SessionId m_sessionId;
        public string m_nickname;
        public int m_numberOfMatches;
        public int m_numberOfWin;
        public int m_numberOfDraw;
        public int m_numberOfDefeats;

        public AIPlayerInfo(MatchUserGameRecord gameRecord) : base(Type.AIPlayerInfo)
        {
            this.m_sessionId = gameRecord.m_sessionId;
            this.m_nickname = gameRecord.m_nickname;
            this.m_numberOfWin = gameRecord.m_numberOfWin;
            this.m_numberOfDraw = gameRecord.m_numberOfDraw;
            this.m_numberOfDefeats = gameRecord.m_numberOfDefeats;
            this.m_numberOfMatches = gameRecord.m_numberOfMatches;
        }

        public MatchUserGameRecord GetMatchRecord()
        {
            MatchUserGameRecord gameRecord = new MatchUserGameRecord();
            gameRecord.m_sessionId = this.m_sessionId;
            gameRecord.m_nickname = this.m_nickname;
            gameRecord.m_numberOfMatches = this.m_numberOfMatches;
            gameRecord.m_numberOfWin = this.m_numberOfWin;
            gameRecord.m_numberOfDraw = this.m_numberOfDraw;
            gameRecord.m_numberOfDefeats = this.m_numberOfDefeats;

            return gameRecord;
        }
    }

    public class LoadRoomSceneMessage : Message
    {
        public LoadRoomSceneMessage() : base(Type.LoadRoomScene)
        {

        }
    }
    public class LoadGameSceneMessage : Message
    {
        public LoadGameSceneMessage() : base(Type.LoadGameScene)
        {

        }
    }

    public class StartCountMessage : Message
    {
        public int time;
        public StartCountMessage(int time) : base(Type.StartCount)
        {
            this.time = time;
        }
    }

    public class GameSyncMessage : Message
    {
        public SessionId host;
        public int count = 0;
        public float[] xPos = null;
        public float[] zPos = null;
        public bool[] onlineInfo = null;

        public GameSyncMessage(SessionId host, int count, float[] x, float[] z, bool[] online) : base(Type.GameSync)
        {
            this.host = host;
            this.count = count;
            this.xPos = new float[count];
            this.zPos = new float[count];
            this.onlineInfo = new bool[count];

            for(int i = 0; i < count; i++)
            {
                xPos[i] = x[i];
                zPos[i] = z[i];
                onlineInfo[i] = online[i];
            }
        }
    }

    public class GameStartMessage : Message
    {
        public GameStartMessage() : base(Type.GameStart)
        {

        }
    }

    public class GameEndMessage : Message
    {
        public int count;
        public int[] sessionList;

        public GameEndMessage(Stack<SessionId> result) : base(Type.GameEnd)
        {
            count = result.Count;
            sessionList = new int[count];
            for(int i = 0; i < count; i++)
            {
                sessionList[i] = (int)result.Pop();
            }
        }
    }

    public class PlayerRotateMessage : Message
    {
        public SessionId playerSession;
        public float xPos;
        public float yPos;
        public float zPos;
        public float xDir;
        public float yDir;
        public float zDir;

        public PlayerRotateMessage(SessionId session, Vector3 pos, Vector3 dir) : base(Type.PlayerRotate)
        {
            this.playerSession = session;
            this.xPos = pos.x;
            this.yPos = pos.y;
            this.zPos = pos.z;
            this.xDir = dir.x;
            this.yDir = dir.y;
            this.zDir = dir.z;
        }
    }

    public class PlayerNoRotateMessage : Message
    {
        public SessionId playerSession;
        public float xPos;
        public float yPos;
        public float zPos;
        public float xDir;
        public float yDir;
        public float zDir;

        public PlayerNoRotateMessage(SessionId session, Vector3 pos, Vector3 dir) : base(Type.PlayerNoRotate)
        {
            this.playerSession = session;
            this.xPos = pos.x;
            this.yPos = pos.y;
            this.zPos = pos.z;
            this.xDir = dir.x;
            this.yDir = dir.y;
            this.zDir = dir.z;
        }
    }
}
