using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSpringJoint : MonoBehaviour
{
    public float SpringPower = 10;
    void Start()
    {
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            var joint = gameObject.transform.GetChild(i).GetComponents<SpringJoint>();

            foreach(var j in joint)
            {
                j.spring = SpringPower;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
