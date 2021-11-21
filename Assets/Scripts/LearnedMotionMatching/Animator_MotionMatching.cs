﻿using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class Animator_MotionMatching : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private GameObject _hips;
    [SerializeField] private GameObject _leftFoot;
    [SerializeField] private GameObject _rightFoot;
    private Vector3 _prevLeftFootPos;
    private Vector3 _prevRightFootPos;
    private Vector3 _leftFootVelocity;
    private Vector3 _rightFootVelocity;
    private Vector3 _hipPosition;
    private Vector3 _hipVelocity;
    private Vector3 _trajectoryPos1;
    private Vector3 _trajectoryPos2;
    private Vector3 _trajectoryVel1;
    private Vector3 _trajectoryVel2;

    private MatchingFeaturesDatabase _database;

    // DEBUG
    private string _featureVectorString;
    private GUIStyle _currentStyle = null;

    // TODO: Have C++ handle calls to Tensorflow models
    [DllImport("MotionMatchingUnityPlugin", CallingConvention = CallingConvention.Cdecl)]
    private static extern int PrintANumber();

    [DllImport("MotionMatchingUnityPlugin", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr PrintHello();

    [DllImport("MotionMatchingUnityPlugin", CallingConvention = CallingConvention.Cdecl)]
    private static extern int AddTwoIntegers(int i1, int i2);

    [DllImport("MotionMatchingUnityPlugin", CallingConvention = CallingConvention.Cdecl)]
    private static extern float AddTwoFloats(float f1, float f2);

    private void Start()
    {
        // TODO: We'll be using this to start playing on a certain spot of a sequence
        animator = this.GetComponent<Animator>();
        // animator.speed = 0f;
        // animator.Play("Walking", 0, 0.5f);
        _database = MatchingFeaturesDatabase.Instance;
        GetCurrentFeatureVector();
    }

    private void Update()
    {
        Vector3 newLeftFootPosition = _leftFoot.transform.position;
        Vector3 newRightFootPosition = _rightFoot.transform.position;
        Vector3 newHipPosition = _hips.transform.position;

        _leftFootVelocity = newLeftFootPosition - _prevLeftFootPos;
        _rightFootVelocity = newRightFootPosition - _prevRightFootPos;
        _hipVelocity = newHipPosition - _hipPosition;

        _prevLeftFootPos = newLeftFootPosition;
        _prevRightFootPos = newRightFootPosition;
        _hipPosition = newHipPosition;

        // TODO: Obtain trajectory positions
        // TODO: Obtain trajectory velocities
        // TODO: Feed feature vector into dictionary

        float[] featureVector = GetCurrentFeatureVector();
        _featureVectorString = PrintUtil.FormatFloatToString(featureVector);
        // Debug.Log(s);
    }

    private void OnGUI()
    {
        InitStyles();
        GUI.Box(new Rect(0, 0, 1000, 20), "Matthew Santiago (mxs123530@utdallas.edu)");
        GUI.Box(new Rect(0, 20, 1000, 20), "CS 6323 - Computer Animation and Gaming Demo");
        GUI.Box(new Rect(0, 40, 1000, 20), "FeatureVector: " + _featureVectorString);
    }

    private void InitStyles()
    {
        if (_currentStyle == null)
        {
            _currentStyle = new GUIStyle(GUI.skin.box);
            _currentStyle.normal.background = MakeTex(2, 2, Color.gray);
        }
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private float[] GetCurrentFeatureVector()
    {
        float[] currentFeatureVector = new float[27];
        // Get feet position
        currentFeatureVector[0] = _leftFoot.transform.position.x;
        currentFeatureVector[1] = _leftFoot.transform.position.y;
        currentFeatureVector[2] = _leftFoot.transform.position.z;
        currentFeatureVector[3] = _rightFoot.transform.position.x;
        currentFeatureVector[4] = _rightFoot.transform.position.y;
        currentFeatureVector[5] = _rightFoot.transform.position.z;
        currentFeatureVector[6] = _leftFootVelocity.x;
        currentFeatureVector[7] = _leftFootVelocity.y;
        currentFeatureVector[8] = _leftFootVelocity.z;
        currentFeatureVector[9] = _rightFootVelocity.x;
        currentFeatureVector[10] = _rightFootVelocity.y;
        currentFeatureVector[11] = _rightFootVelocity.z;
        currentFeatureVector[12] = _hipVelocity.x;
        currentFeatureVector[13] = _hipVelocity.y;
        currentFeatureVector[14] = _hipVelocity.z;
        // TODO: Obtain trajectory positions
        currentFeatureVector[15] = animator.velocity.x;
        currentFeatureVector[16] = animator.velocity.y;
        currentFeatureVector[17] = animator.velocity.z;
        // TODO: Obtain trajectory velocities
        currentFeatureVector[18] = animator.velocity.x;
        currentFeatureVector[19] = animator.velocity.y;
        currentFeatureVector[20] = animator.velocity.z;
        return currentFeatureVector;
    }
}