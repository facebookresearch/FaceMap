using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAfterTimePeriod : MonoBehaviour
{
    public float time = 1;

    // Start is called before the first frame update
    void Start()
    {
        GameObject.Destroy(gameObject, time);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
