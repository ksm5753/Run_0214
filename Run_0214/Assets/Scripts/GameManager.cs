using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;

    #region Scene
    private const string LOGIN = "1. Login";
    private const string LOBBY = "2. Lobby";
    private const string READYROOM = "3. ReadyRoom";
    private const string INGAME = "4. InGame";
    #endregion

    #region Actions-Events
    public static event Action InGame = delegate { };
    public static event Action AfterInGame = delegate { };

    #endregion

    private string asyncSceneName = string.Empty;

    private IEnumerator InGameUpdateCoroutine;

    public enum GameState { Login, Lobby, Ready, Start, InGame };
    private GameState gameState;

    public static GameManager GetInstance()
    {
        if(instance == null)
        {
            Debug.LogError("GameManager 인스턴스가 존재하지 않습니다.");
            return null;
        }
        return instance;
    }

    void Awake()
    {
        if (!instance) instance = this;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        DontDestroyOnLoad(this.gameObject);

        InGameUpdateCoroutine = InGameUpdate();
    }

    void Start()
    {
        gameState = GameState.Login;
    }

    private void Update()
    {
        if(gameState == GameState.InGame)
        {
            //InGame();
            //AfterInGame();
        }
    }

    IEnumerator InGameUpdate()
    {
        while (true)
        {
            if(gameState != GameState.InGame)
            {
                StopCoroutine(InGameUpdateCoroutine);
                yield return null;
            }
            InGame();
            AfterInGame();
            yield return new WaitForSeconds(.1f);
        }
    }

    public void ChangeState(GameState state, Action<bool> func = null)
    {
        gameState = state;
        switch (gameState)
        {
            case GameState.Login:
                break;
            case GameState.Lobby:
                Lobby(func);
                break;
            case GameState.Ready:
                GameReady();
                break;
            case GameState.Start:
                GameStart();
                break;
            case GameState.InGame:
                StartCoroutine(InGameUpdateCoroutine);
                break;
        }
    }

    private void Lobby(Action<bool> func)
    {
        if (func != null) ChangeSceneAsync(LOBBY, func);
        else ChangeScene(LOBBY);
    }

    private void GameReady()
    {
        print("GameMangaer.cs - GameReady - 게임 레디 상태 돌입");
        ChangeScene(READYROOM);
    }

    private void GameStart()
    {
        ChangeScene(INGAME);
    }

    //=========================================================================
    private void ChangeScene(string scene)
    {
        SceneManager.LoadScene(scene);
    }

    private void ChangeSceneAsync(string scene, Action<bool> func)
    {
        asyncSceneName = scene;

        StartCoroutine("LoadScene", func);
    }

    private IEnumerator LoadScene(Action<bool> func)
    {
        var asyncScene = SceneManager.LoadSceneAsync(asyncSceneName);
        asyncScene.allowSceneActivation = true;

        bool isCallFunc = false;
        while (!asyncScene.isDone)
        {
            if (asyncScene.progress <= 0.9f)
                func(false);
            else if (!isCallFunc)
            {
                isCallFunc = true;
                func(true);
            }
            yield return null;
        }
    }
}
