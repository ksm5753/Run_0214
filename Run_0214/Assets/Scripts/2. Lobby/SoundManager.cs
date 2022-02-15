using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public AudioSource bgmSource;

    void Update()
    {
        bgmSource.mute = PlayerPrefs.GetInt("BGM_Mute") == 1 ? false : true;
    }
    

    public void SetBGM(bool check)
    {
        bgmSource.mute = check;
        PlayerPrefs.SetInt("BGM_Mute", (check) ? 1 : 0);
    }
}
