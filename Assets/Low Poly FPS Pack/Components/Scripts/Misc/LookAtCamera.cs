using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private void Start()
    {
#if !UNITY_SERVER
        //Fix inverted scale issue
        gameObject.transform.localScale =
            new Vector3(-1, 1, 1);
#endif
    }

    private void Update()
    {
#if !UNITY_SERVER
        //Object always face camera
        transform.LookAt(Camera.main.transform);
#endif
    }
}