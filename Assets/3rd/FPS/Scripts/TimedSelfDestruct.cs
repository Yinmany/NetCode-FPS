using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedSelfDestruct : MonoBehaviour
{
    public float lifeTime = 1f;

    float m_spawnTime;

    void Awake()
    {
        m_spawnTime = Time.time;
    }

    private void Update()
    {
        if(Time.time > m_spawnTime + lifeTime)
        {
            Destroy(gameObject);
        }
    }
}
