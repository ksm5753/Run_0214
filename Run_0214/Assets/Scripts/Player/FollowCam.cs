using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCam : MonoBehaviour
{
    string Origin = "";
    public int Distance = 10;

    void Start()
    {
        Origin = GetComponent<TextMesh>().text;
    }

    void Update()
    {
        float dist = Vector3.Distance(GameObject.Find("Main Camera").transform.position, transform.position);
        if (dist > Distance) GetComponent<TextMesh>().text = "";
        else GetComponent<TextMesh>().text = Origin;

        transform.rotation = GameObject.Find("Main Camera").transform.rotation;
    }
}
