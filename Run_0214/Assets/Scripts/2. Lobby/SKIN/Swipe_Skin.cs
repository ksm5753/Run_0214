using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Swipe_Skin : MonoBehaviour
{
    public GameObject scrollBar;
    public float scrollPos = 0;
    public int currentPos;
    private float[] pos;

    void Start()
    {
        pos = new float[transform.childCount];
        for(int i = 0; i < pos.Length; i++)
        {
            int temp = i;
            transform.GetChild(temp).GetComponent<Button>().onClick.AddListener(() => SetScroll(temp));
        }
    }

    void SetScroll(int num)
    {
        print("AAA " + num);
        scrollPos = num * 0.25f;
    }

    void Update()
    {
        pos = new float[transform.childCount];
        float distance = 1f / (pos.Length - 1);

        for(int i = 0; i < pos.Length; i++)
        {
            pos[i] = distance * i;
            transform.GetChild(i).GetComponentInChildren<Text>().text = i.ToString();

            if(scrollPos < pos[i] + (distance / 2) && scrollPos > pos[i] - (distance / 2))
            {
                currentPos = i;
                transform.GetChild(i).localScale = Vector2.Lerp(transform.GetChild(i).localScale, new Vector2(1.2f, 1.2f), 0.1f);

                for(int j = 0; j < pos.Length; j++)
                {
                    if (j != i)
                        transform.GetChild(j).localScale = Vector2.Lerp(transform.GetChild(j).localScale, new Vector2(0.8f, 0.8f), 0.1f);
                }
            }
        }

        if (Input.GetMouseButton(0))
        {
            scrollPos = scrollBar.GetComponent<Scrollbar>().value;
        }
        else
        {
            for(int i = 0; i < pos.Length; i++)
            {
                if(scrollPos < pos[i] + (distance / 2) && scrollPos > pos[i] - (distance / 2))
                {
                    scrollBar.GetComponent<Scrollbar>().value = Mathf.Lerp(scrollBar.GetComponent<Scrollbar>().value, pos[i], 0.1f);
                }
            }
        }
    }
}
