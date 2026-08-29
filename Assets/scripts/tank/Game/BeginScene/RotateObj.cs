using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateObj : MonoBehaviour
{
    // Start is called before the first frame update
    public float rotateSeed = 5;
    // Update is called once per frame
    void Update()
    {
        this.transform.Rotate(Vector3.up,rotateSeed*Time.deltaTime);
    }
}
