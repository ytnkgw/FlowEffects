using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Skinner;

public class InternalTest : MonoBehaviour {

    enum Test1
    {
        Test,
        Test01
    }

    enum Test2
    {
        Test,
        Test01
    }

    private SkinnerTrail skinner;
    private AnimationKernelSet<Test1, Test2> internals;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
