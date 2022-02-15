using System;
using System.Collections.Generic;
using UnityEngine;
// Include Backend
using BackEnd;
using static BackEnd.SendQueue;
//  Include GPGS namespace
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine.SocialPlatforms;
using LitJson;
#if UNITY_IOS
using UnityEngine.SignInWithApple;
#endif

public class RankItem //Rank Class
{
    public string nickname { get; set; } // 닉네임
    public string score { get; set; }    // 점수
    public string rank { get; set; }     // 랭크
}

public class BackendServerManager : MonoBehaviour
{

    private static BackendServerManager instance;   // 인스턴스

    private string tempNickName;                        // 설정할 닉네임 (id와 동일)
    public string myNickName { get; private set; } = string.Empty;  // 로그인한 계정의 닉네임
    public string myIndate { get; private set; } = string.Empty;    // 로그인한 계정의 inDate
    private Action<bool, string> loginSuccessFunc = null;

    private const string BackendError = "statusCode : {0}\nErrorCode : {1}\nMessage : {2}";

    public string appleToken = ""; // SignInWithApple.cs에서 토큰값을 받을 문자열

    string userIndate;


    public string rankUuid = "";


    //=================================================================================================
    #region 서버 초기화
    void Initialize()
    {
#if UNITY_ANDROID
        PlayGamesClientConfiguration config = new PlayGamesClientConfiguration
            .Builder()
            .RequestServerAuthCode(false)
            .RequestIdToken()
            .Build();
        PlayGamesPlatform.InitializeInstance(config);
        PlayGamesPlatform.DebugLogEnabled = false;

        PlayGamesPlatform.Activate();
#endif

        var bro = Backend.Initialize(true);

        if (bro.IsSuccess())
        {

#if UNITY_ANDROID //안드로이드에서만 작동
            Debug.Log("GoogleHash - " + Backend.Utils.GetGoogleHash());
#endif
#if !UNITY_EDITOR //안드로이드, iOS 환경에서만 작동
            GetVersionInfo();
#endif
        }
        else Debug.LogError("뒤끝 초기화 실패 : " + bro);
    }
    #endregion

    #region 버전 확인 (모바일)
    private void GetVersionInfo()
    {
        Enqueue(Backend.Utils.GetLatestVersion, callback =>
        {
            if (callback.IsSuccess() == false)
            {
                Debug.LogError("버전정보를 불러오는 데 실패하였습니다.\n" + callback);
                return;
            }

            var version = callback.GetReturnValuetoJSON()["version"].ToString();

            Version server = new Version(version);
            Version client = new Version(Application.version);

            var result = server.CompareTo(client);
            if (result == 0)
            {
                // 0 이면 두 버전이 일치
                return;
            }
            else if (result < 0)
            {
                // 0 미만이면 server 버전이 client 이전 버전
                // 검수를 넣었을 경우 여기에 해당된다.
                // ex) 검수버전 3.0.0, 라이브에 운용되고 있는 버전 2.0.0, 콘솔 버전 2.0.0
                return;
            }
            else
            {
                // 0보다 크면 server 버전이 client 이후 버전
                if (client == null)
                {
                    // 클라이언트가 null인 경우 예외처리
                    Debug.LogError("클라이언트 버전정보가 null 입니다.");
                    return;
                }
            }

            // 버전 업데이트 팝업
            //LoginUI.GetInstance().OpenUpdatePopup();
        });
    }
    #endregion

    #region 토큰으로 로그인
    public void BackendTokenLogin(Action<bool, string> func)
    {
        Enqueue(Backend.BMember.LoginWithTheBackendToken, callback =>
        {
            if (callback.IsSuccess())
            {
                Debug.Log("토큰 로그인 성공");
                loginSuccessFunc = func; //로그인 성공 여부 데이터 입력

                OnBackendAuthorized(); //사전 유저 불러오기
                return;
            }

            Debug.Log("토큰 로그인 실패\n" + callback.ToString());
            func(false, string.Empty); //실패시 토큰 초기화
        });
    }
    #endregion

    #region 커스텀 로그인
    public void CustomLogin(string id, string pw, Action<bool, string> func)
    {
        Enqueue(Backend.BMember.CustomLogin, id, pw, callback =>
        {
            if (callback.IsSuccess())
            {
                Debug.Log("커스텀 로그인 성공");
                loginSuccessFunc = func; //로그인 여부 데이터 입력

                OnBackendAuthorized(); //사전 유저 정보 불러오기
                return;
            }

            Debug.Log("커스텀 로그인 실패\n" + callback);
            func(false, string.Format(BackendError,
                callback.GetStatusCode(), callback.GetErrorCode(), callback.GetMessage()));
        });
    }
    #endregion

    #region 커스텀 회원가입
    public void CustomSignIn(string id, string pw, Action<bool, string> func)
    {
        tempNickName = id;
        Enqueue(Backend.BMember.CustomSignUp, id, pw, callback =>
        {
            if (callback.IsSuccess())
            {
                Debug.Log("커스텀 회원가입 성공");
                loginSuccessFunc = func; //로그인 여부 데이터 입력

                OnBackendAuthorized(); //사전 유저 정보 불러오기
                return;
            }

            Debug.LogError("커스텀 회원가입 실패\n" + callback.ToString());
            func(false, string.Format(BackendError,
                callback.GetStatusCode(), callback.GetErrorCode(), callback.GetMessage()));
        });
    }
    #endregion

    #region 구글 페더레이션 로그인 / 회원가입
    public void GoogleAuthorizeFederation(Action<bool, string> func)
    {
#if UNITY_ANDROID //안드로이드 일 경우

        if (Social.localUser.authenticated) // 이미 gpgs 로그인이 된 경우
        {
            var token = GetFederationToken();
            if (token.Equals(string.Empty)) //토큰이 존재하지 않을 경우
            {
                Debug.LogError("GPGS 토큰이 존재하지 않습니다.");
                func(false, "GPGS 인증을 실패하였습니다.\nGPGS 토큰이 존재하지 않습니다.");
                return;
            }

            Enqueue(Backend.BMember.AuthorizeFederation, token, FederationType.Google, "gpgs 인증", callback =>
            {
                if (callback.IsSuccess())
                {
                    Debug.Log("GPGS 인증 성공");
                    loginSuccessFunc = func;

                    OnBackendAuthorized(); //사전 유저 정보 불러오기
                    return;
                }

                Debug.LogError("GPGS 인증 실패\n" + callback.ToString());
                func(false, string.Format(BackendError,
                    callback.GetStatusCode(), callback.GetErrorCode(), callback.GetMessage()));
            });
        }

        else // gpgs 로그인을 해야하는 경우
        {
            Social.localUser.Authenticate((bool success) =>
            {
                if (success)
                {
                    var token = GetFederationToken();
                    if (token.Equals(string.Empty))
                    {
                        Debug.LogError("GPGS 토큰이 존재하지 않습니다.");
                        func(false, "GPGS 인증을 실패하였습니다.\nGPGS 토큰이 존재하지 않습니다.");
                        return;
                    }

                    Enqueue(Backend.BMember.AuthorizeFederation, token, FederationType.Google, "gpgs 인증", callback =>
                    {
                        if (callback.IsSuccess())
                        {
                            Debug.Log("GPGS 인증 성공");
                            loginSuccessFunc = func;

                            OnBackendAuthorized(); //사전 유저 정보 불러오기
                            return;
                        }

                        Debug.LogError("GPGS 인증 실패\n" + callback.ToString());
                        func(false, string.Format(BackendError,
                            callback.GetStatusCode(), callback.GetErrorCode(), callback.GetMessage()));
                    });
                }
                else
                {
                    Debug.LogError("GPGS 로그인 실패");
                    func(false, "GPGS 인증을 실패하였습니다.\nGPGS 로그인을 실패하였습니다.");
                    return;
                }
            });
        }
#endif
    }
    #endregion

    #region 애플 페더레이션 로그인 / 회원가입
    public void AppleAuthorizeFederation(Action<bool, string> func)
    {
#if UNITY_IOS
        loginSuccessFunc = func;
        var siwa = gameObject.GetComponent<SignInWithApple>();
        siwa.Login(AppleFedeCallback);
#endif
    }

#if UNITY_IOS
    private void AppleFedeCallback(SignInWithApple.CallbackArgs args)
    {
        Debug.Log("애플 토큰으로 뒤끝에 로그인");
        Enqueue(Backend.BMember.AuthorizeFederation, appleToken, FederationType.Apple, "apple 인증", callback =>
        {
            if (callback.IsSuccess())
            {
                Debug.Log("Apple 인증 성공");

                OnBackendAuthorized();
                return;
            }

            Debug.LogError("Apple 인증 실패\n" + callback.ToString());
            loginSuccessFunc(false, string.Format(BackendError,
                callback.GetStatusCode(), callback.GetErrorCode(), callback.GetMessage()));
        });

    }
#endif
    #endregion

    #region 구글 토큰 받기
    private string GetFederationToken()
    {
#if UNITY_ANDROID
        if (!PlayGamesPlatform.Instance.localUser.authenticated)
        {
            Debug.LogError("GPGS에 접속되어있지 않습니다. PlayGamesPlatform.Instance.localUser.authenticated :  fail");
            return string.Empty;
        }
        // 유저 토큰 받기
        string _IDtoken = PlayGamesPlatform.Instance.GetIdToken();
        tempNickName = PlayGamesPlatform.Instance.GetUserDisplayName();
        Debug.Log(tempNickName);
        return _IDtoken;

#elif UNITY_IOS
        return string.Empty;
#else
        return string.Empty;
#endif
    }
    #endregion

    #region 닉네임 값 입력
    public void UpdateNickname(string nickname, Action<bool, string> func)
    {
        Enqueue(Backend.BMember.UpdateNickname, nickname, bro =>
        {
            // 닉네임이 없으면 매치서버 접속이 안됨
            if (!bro.IsSuccess())
            {
                Debug.LogError("닉네임 생성 실패\n" + bro.ToString());
                func(false, string.Format(BackendError,
                    bro.GetStatusCode(), bro.GetErrorCode(), bro.GetMessage()));
                return;
            }
            loginSuccessFunc = func;
            OnBackendAuthorized(); //유저 사전 정보 불러오기
        });
    }
    #endregion

    #region 게스트 로그인
    public void GuestLogin(Action<bool, string> func)
    {
        Enqueue(Backend.BMember.GuestLogin, callback =>
        {
            if (callback.IsSuccess())
            {
                Debug.Log("게스트 로그인 성공");
                loginSuccessFunc = func;

                OnBackendAuthorized(); //유저 사전 정보 불러오기
                return;
            }

            Debug.Log("게스트 로그인 실패\n" + callback);
            func(false, string.Format(BackendError,
                callback.GetStatusCode(), callback.GetErrorCode(), callback.GetMessage()));
        });
    }
    #endregion

    #region 실제 유저 정보 불러오기
    private void OnBackendAuthorized()
    {
        Enqueue(Backend.BMember.GetUserInfo, callback =>
        {
            if (!callback.IsSuccess())
            {
                Debug.LogError("유저 정보 불러오기 실패\n" + callback);
                loginSuccessFunc(false, string.Format(BackendError,
                callback.GetStatusCode(), callback.GetErrorCode(), callback.GetMessage()));
                return;
            }
            Debug.Log("유저정보\n" + callback);

            var info = callback.GetReturnValuetoJSON()["row"];
            

            if (info["nickname"] == null) //닉네임 값이 없을 경우 닉네임 적는 UI띄우기
            {
                LoginUI.GetInstance().ActiveNickNameObject();
                return;
            }
            myNickName = info["nickname"].ToString();
            myIndate = info["inDate"].ToString();

            if (loginSuccessFunc != null) //로그인 성공했으므로 매칭 리스트 값 불러옴
                BackendMatchManager.GetInstance().GetMatchList(loginSuccessFunc);
        });
    }
    #endregion

    //=================================================================================================

    //=================================================================================================


    //=================================================================================================


    #region 게임 처음 초기화
    public void InitalizeGameData()
    {
        Enqueue(Backend.GameData.Get, "User", new Where(), callback =>
        {
            if (callback.IsSuccess())
            {
                JsonData userData = callback.GetReturnValuetoJSON()["rows"];
                if (userData.Count == 0)
                {
                    Param param = new Param();

                    param.Add("Gold", 0);
                    param.Add("Diamond", 0);

                    param.Add("AD", 5);

                    Enqueue(Backend.GameData.Insert, "User", param, (callback) =>
                    {
                        if (callback.IsSuccess()) print("성공");
                        else print("실패");
                    });

                    Param param2 = new Param();
                    for (int i = 0; i < 100; i++)
                    {
                        param2.Add("Card " + string.Format("{0:D3}", (i + 1)), 0);
                    }

                    for (int i = 1; i <= 5; i++)
                    {
                        Enqueue(Backend.GameData.Insert, "Option" + i, param2, (callback) =>
                        {
                            if (callback.IsSuccess()) print("성공");
                            else print("실패");
                        });
                    }
                }
                userIndate = userData[0]["inDate"]["S"].ToString();
            }
        });
    }
    #endregion

    void Awake()
    {
        if (instance != null)
        {
            Destroy(instance);
        }
        instance = this;
        // 모든 씬에서 유지
        DontDestroyOnLoad(this.gameObject);
    }

    public static BackendServerManager GetInstance()
    {
        if (instance == null)
        {
            Debug.LogError("BackendServerManager 인스턴스가 존재하지 않습니다.");
            return null;
        }

        return instance;
    }

    void Start()
    {
        Initialize(); //서버 초기화
    }

    void Update()
    {
        //비동기함수 풀링
        Backend.AsyncPoll();
    }

}