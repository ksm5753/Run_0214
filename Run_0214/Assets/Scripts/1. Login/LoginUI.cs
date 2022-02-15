using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Battlehub.Dispatcher;


public class LoginUI : MonoBehaviour
{
    //로그인 창
    public GameObject[] Steps;

    [Space(10f)]
    [Header("로그인 팝업창")]
    public GameObject customLoginObject; //커스텀 로그인
    public GameObject signUpObject; //회원 가입
    public GameObject nicknameObject; //닉네임 창
    [Space(10f)]
    [Header("로그인")]
    public InputField[] loginField;
    public InputField[] signField;
    public InputField nicknameField;
    [Space(10f)]
    [Header("기타")]
    public GameObject errorObject;
    public GameObject loadingObject;

    private static LoginUI instance;
    public static LoginUI GetInstance()
    {
        if (instance == null) return null;

        return instance;
    }

    void Awake()
    {
        if (!instance) instance = this;
    }

    void Start()
    {
        loadingObject.SetActive(false);
    }

    #region 초기화 (ScaleCtrl -> Initialize)
    public void Initialize()
    {
        loadingObject.SetActive(false);
        errorObject.SetActive(false);
        for (int i = 1; i < Steps.Length; i++) //터치스크린 제외 모두 끄기
            Steps[i].SetActive(false);
    }
    #endregion


    //==========================================================================
    #region 터치 후 자동로그인
    public void TouchStart()
    {
        // 업데이트 팝업이 떠있으면 진행 X
        //if (updateObject.activeSelf == true) return;

        loadingObject.SetActive(true);
        BackendServerManager.GetInstance().BackendTokenLogin((bool result, string error) =>
        {
            Dispatcher.Current.BeginInvoke(() =>
            {
                if (result)
                {
                    ChangeLobbyScene();
                    return;
                }
                loadingObject.SetActive(false);
                if (!error.Equals(string.Empty))
                {
                    errorObject.GetComponentInChildren<Text>().text = "유저 정보 불러오기 실패\n\n" + error;
                    errorObject.SetActive(true);
                    return;
                }
                
                for(int i = 0; i < Steps.Length; i++)
                {
                    Steps[i].SetActive(false);
                }
                Steps[1].SetActive(true); //로그인
            });
        });
    }
    #endregion
    //==========================================================================
    #region 커스텀 로그인
    public void Login()
    {
        if (errorObject.activeSelf)
        {
            return;
        }
        string id = loginField[0].text;
        string pw = loginField[1].text;

        if (id.Equals(string.Empty) || pw.Equals(string.Empty))
        {
            errorObject.GetComponentInChildren<Text>().text = "ID 혹은 PW 를 먼저 입력해주세요.";
            errorObject.SetActive(true);
            return;
        }

        loadingObject.SetActive(true);
        BackendServerManager.GetInstance().CustomLogin(id, pw, (bool result, string error) =>
        {
            Dispatcher.Current.BeginInvoke(() =>
            {
                if (!result)
                {
                    loadingObject.SetActive(false);
                    errorObject.GetComponentInChildren<Text>().text = "로그인 에러\n\n" + error;
                    errorObject.SetActive(true);
                    return;
                }
                ChangeLobbyScene();
            });
        });
    }
    #endregion
    //==========================================================================
    #region 커스텀 회원가입
    public void SignUp()
    {
        if (errorObject.activeSelf)
        {
            return;
        }
        string id = signField[0].text;
        string pw = signField[1].text;

        if (id.Equals(string.Empty) || pw.Equals(string.Empty))
        {
            errorObject.GetComponentInChildren<Text>().text = "ID 혹은 PW 를 먼저 입력해주세요.";
            errorObject.SetActive(true);
            return;
        }

        loadingObject.SetActive(true);
        BackendServerManager.GetInstance().CustomSignIn(id, pw, (bool result, string error) =>
        {
            Dispatcher.Current.BeginInvoke(() =>
            {
                if (!result)
                {
                    loadingObject.SetActive(false);
                    errorObject.GetComponentInChildren<Text>().text = "회원가입 에러\n\n" + error;
                    errorObject.SetActive(true);
                    return;
                }
                ChangeLobbyScene();
            });
        });
    }
    #endregion
    //==========================================================================
    #region 닉네임 활성
    public void ActiveNickNameObject()
    {
        Dispatcher.Current.BeginInvoke(() =>
        {
            for(int i = 0; i < Steps.Length; i++)
            {
                Steps[i].SetActive(false);
            }
            errorObject.SetActive(false);
            loadingObject.SetActive(false);
            Steps[Steps.Length - 1].SetActive(true);
        });
    }
    #endregion
    //==========================================================================
    #region 닉네임 설정
    public void UpdateNickName()
    {
        if (errorObject.activeSelf)
        {
            return;
        }
        string nickname = nicknameField.text;
        if (nickname.Equals(string.Empty))
        {
            errorObject.GetComponentInChildren<Text>().text = "닉네임을 먼저 입력해주세요";
            errorObject.SetActive(true);
            return;
        }
        loadingObject.SetActive(true);
        BackendServerManager.GetInstance().UpdateNickname(nickname, (bool result, string error) =>
        {
            Dispatcher.Current.BeginInvoke(() =>
            {
                if (!result)
                {
                    loadingObject.SetActive(false);
                    errorObject.GetComponentInChildren<Text>().text = "닉네임 생성 오류\n\n" + error;
                    errorObject.SetActive(true);
                    return;
                }
                ChangeLobbyScene();
            });
        });
    }
    #endregion
    //==========================================================================
    #region 구글 로그인
    public void GoogleFederation()
    {
        if (errorObject.activeSelf)
        {
            return;
        }

        loadingObject.SetActive(true);
        BackendServerManager.GetInstance().GoogleAuthorizeFederation((bool result, string error) =>
        {
            Dispatcher.Current.BeginInvoke(() =>
            {
                if (!result)
                {
                    loadingObject.SetActive(false);
                    errorObject.GetComponentInChildren<Text>().text = "로그인 에러\n\n" + error;
                    errorObject.SetActive(true);
                    return;
                }
                ChangeLobbyScene();
            });
        });
    }
    #endregion
    //==========================================================================
    #region 애플 로그인
    public void AppleFederation()
    {
        if (errorObject.activeSelf)
        {
            return;
        }

        loadingObject.SetActive(true);
        BackendServerManager.GetInstance().AppleAuthorizeFederation((bool result, string error) =>
        {
            Dispatcher.Current.BeginInvoke(() =>
            {
                if (!result)
                {
                    loadingObject.SetActive(false);
                    errorObject.GetComponentInChildren<Text>().text = "로그인 에러\n\n" + error;
                    errorObject.SetActive(true);
                    return;
                }
                ChangeLobbyScene();
            });
        });
    }
    #endregion
    //==========================================================================
    #region 게스트 로그인
    public void GuestLogin()
    {
        if (errorObject.activeSelf)
        {
            return;
        }

        loadingObject.SetActive(true);
        BackendServerManager.GetInstance().GuestLogin((bool result, string error) =>
        {
            Dispatcher.Current.BeginInvoke(() =>
            {
                if (!result)
                {
                    loadingObject.SetActive(false);
                    errorObject.GetComponentInChildren<Text>().text = "로그인 에러\n\n" + error;
                    errorObject.SetActive(true);
                    return;
                }
                ChangeLobbyScene();
            });
        });
    }
    #endregion
    //==========================================================================
    void ChangeLobbyScene()
    {
        GameManager.GetInstance().ChangeState(GameManager.GameState.Lobby);
    }
    //==========================================================================
}
