using BackEnd;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class LobbyUI : MonoBehaviour
{
    [Header("Main Panel")]
    public Toggle[] MenuButton;
    public GameObject mainParent;
    public Scrollbar scrollBar;                 //scrollBar의 위치를 바탕으로 현재 페이지 검사
    public float swipeTime = 0.01f;             //페이지가 Swipe 되는 시간
    public float swipeDistance = 50.0f;         //페이지가 Swipe되기 위해 움직여야 하는 최소 거리

    [Space(15f)]
    [Header("Home Panel")]
    public GameObject NicknamePanel;            

    private float[] scrollPageValues;           //각 페이지의 위치 값 [0.0 - 1.0]
    private float valueDistance = 0;            //각 페이지 사이의 거리
    private int currentPage = 0;                //현재 페이지
    private int maxPage = 0;                    //최대 페이지
    private float startTouchX;                  //터치 시작 위치
    private float endTouchX;                    //터치 종료 위치
    private bool isSwipeMode = false;           //현재 Swipe가 되고 있는지 체크

    [Space(15f)]
    [Header("Others")]
    public GameObject matchRequestPanel;
    public GameObject matchDonePanel;
    public GameObject loadingObject;
    public GameObject errorObject;

    private static LobbyUI instance;


    #region 초기화 (ScaleCtrl -> Initialize)
    public void Initialize()
    {
        loadingObject.SetActive(false);
        errorObject.SetActive(false);

    }
    #endregion

    void Start()
    {
        if (BackendServerManager.GetInstance() != null) setNickName(); //로비 들어오면 닉네임 설정
                                           
        BackendServerManager.GetInstance().InitalizeGameData(); //게임데이터 초기설정

        SetscrollBarValue(1, false);
        
    }

    void Update()
    {
        UpdateInput();
    }

    #region 닉네임 설정
    private void setNickName()
    {
        var name = BackendServerManager.GetInstance().myNickName;
        if (name.Equals(string.Empty))
        {
            Debug.Log("닉네임 불러오기 실패");
            name = "Error";
        }

        NicknamePanel.GetComponentInChildren<Text>().text = name;
    }
    #endregion

    //===========================================================================================
    #region 메인 화면 설정
    public void SetscrollBarValue(int index,  bool mode = true)
    {
        currentPage = index;

        if(!mode)
            scrollBar.value = scrollPageValues[index];

        StartCoroutine(OnSwipeOneStep(currentPage));
    }

    private void UpdateInput()
    {
        //현재 Swipe를 진행중이면 터치 불가
        if (isSwipeMode) return;

#if UNITY_EDITOR
        //마우스 왼쪽 버튼을 눌렀을 때 1회
        if (Input.GetMouseButtonDown(0))
        {
            //터치 시작 지점(Swipe 방향 구분)
            startTouchX = Input.mousePosition.x;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            //터치 종료 지점 (Swipe 방향 구분)
            endTouchX = Input.mousePosition.x;
            UpdateSwipe();
        }
#endif

#if UNITY_ANDROID
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                //터치 시작 지점(Swipe 방향 구분)
                startTouchX = Input.mousePosition.x;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                //터치 종료 지점 (Swipe 방향 구분)
                endTouchX = Input.mousePosition.x;
                UpdateSwipe();
            }
        }
#endif
    }

    private void UpdateSwipe()
    {
        //너무 작은 거리를 움직였을 때는 Swipe X
        if (Mathf.Abs(startTouchX - endTouchX) < swipeDistance)
        {
            //원래 페이지로 Swipe해서 돌아간다
            StartCoroutine(OnSwipeOneStep(currentPage));
            return;
        }

        //Swipe 방향
        bool isLeft = startTouchX < endTouchX ? true : false;

        //이동 방향이 왼쪽일 때
        if (isLeft)
        {
            //현재 페이지가 왼쪽 끝이면 종료
            if (currentPage == 0) return;

            //왼쪽으로 이동을 위해 현재 페이지를 1 감소
            currentPage--;
        }

        //이동 방향이 오른쪽일 때
        else
        {
            //현재 페이지가 오른쪽 끝이면 종료
            if (currentPage == maxPage - 1) return;

            //오른쪽으로 이동을 위해 현재 페이지를 1 증가
            currentPage++;
        }

        //currentIndex 번째 페이지로 Swipe해서 이동
        StartCoroutine(OnSwipeOneStep(currentPage));
    }

    private IEnumerator OnSwipeOneStep(int index)
    {
        float start = scrollBar.value;
        float current = 0;
        float percent = 0;

        isSwipeMode = true;

        while (percent < 1)
        {
            current += Time.deltaTime;
            percent = current / swipeTime;

            scrollBar.value = Mathf.Lerp(start, scrollPageValues[index], percent);

            MenuButton[currentPage].isOn = true;
            yield return null;
        }

        isSwipeMode = false;
    }

    public void MoveMenu(Toggle toggle)
    {
        if (toggle.isOn)
        {
            switch (toggle.name)
            {
                case "Shop":
                    SetscrollBarValue(0);
                    break;
                case "Home":
                    SetscrollBarValue(1);
                    break;
                case "Setting":
                    SetscrollBarValue(2);
                    break;
            }
        }
    }
    #endregion
    //===========================================================================================

    public void SetLoadingObjectActive(bool isActive)
    {
        loadingObject.SetActive(isActive);
    }

    public void ChangeRoomLoadScene()
    {
        GameManager.GetInstance().ChangeState(GameManager.GameState.Ready);
    }



    public static LobbyUI GetInstance()
    {
        if (instance == null) return null;

        return instance;
    }

    void Awake()
    {
        if (!instance) instance = this;

        BackendMatchManager.GetInstance().IsMatchGameActivate();

        //스크롤 되는 페이지의 각 value 값을 저장하는 배열 메모리 할당
        scrollPageValues = new float[mainParent.transform.childCount];

        //스크롤 되는 페이지 사이의 거리
        valueDistance = 1f / (scrollPageValues.Length - 1f);

        //스크롤 되는 페이지의 각 value 위치 설정 [0 <= value <= 1]
        for (int i = 0; i < scrollPageValues.Length; i++)
            scrollPageValues[i] = valueDistance * i;

        //최대 페이지의 수
        maxPage = mainParent.transform.childCount;
    }

}
