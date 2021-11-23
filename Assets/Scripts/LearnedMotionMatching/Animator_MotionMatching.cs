using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class Animator_MotionMatching : MonoBehaviour
{
    private Animator _animator;
    // [SerializeField] private GameObject _hips = null;
    // [SerializeField] private GameObject _leftFoot = null;
    // [SerializeField] private GameObject _rightFoot = null;
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

    private void Start()
    {
        // TODO: We'll be using this to start playing on a certain spot of a sequence
        _animator = this.GetComponent<Animator>();
        _database = MatchingFeaturesDatabase.Instance;

        // Load all feature vectors
        _animator.applyRootMotion = true;
        _animator.speed = 0;
        // foreach (AnimationClip ac in _animator.runtimeAnimatorController.animationClips)
        // {
        //     int numFrames = (int)(ac.frameRate * ac.length);
        //     for (int i = 0; i < numFrames; i++)
        //     {
        //         float t = (float)(i / numFrames);
        //         _animator.Play("Base Layer." + ac.name, 0, t);
        //         SetFeatures(ac.frameRate);
        //         float[] featureVector = GetCurrentFeatureVector();
        //         _database.AddToDatabase(ac.name, featureVector);
        //     }
        // }
        // _database.Print();

        _animator.applyRootMotion = false;
        _animator.speed = 1;
        // _animator.Play("Base Layer.Idle", 0, 0); // Resume from beginning
    }

    private void Update()
    {
        // (string name, float normalizedTime) t = _database.GetClosestFeature(featureVector);
        // _animator.Play(t.name, 0, t.normalizedTime);

        // Debug.Log(s);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        SetFeatures(Time.deltaTime);
        float[] featureVector = GetCurrentFeatureVector();
        _featureVectorString = PrintUtil.FormatFloatToString(featureVector);
    }

    private void SetFeatures(float dt)
    {
        Vector3 newLeftFootPosition = _animator.GetIKPosition(AvatarIKGoal.LeftFoot);
        Vector3 newRightFootPosition = _animator.GetIKPosition(AvatarIKGoal.RightFoot);
        Vector3 newHipPosition = _animator.rootPosition;

        Debug.Log("newLeftFootPosition: " + newLeftFootPosition);

        // Velocities are small.. we may have to do this for all the animations
        _leftFootVelocity = (newLeftFootPosition - _prevLeftFootPos) / dt;
        _rightFootVelocity = (newRightFootPosition - _prevRightFootPos) / dt;
        _hipVelocity = (newHipPosition - _hipPosition) / dt;

        _prevLeftFootPos = newLeftFootPosition;
        _prevRightFootPos = newRightFootPosition;
        _hipPosition = newHipPosition;

        // TODO: Obtain trajectory positions
        // TODO: Obtain trajectory velocities
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
        // Calculate true local position - feet 
        Vector3 leftFootPosition = _animator.GetIKPosition(AvatarIKGoal.LeftFoot);
        Vector3 rightFootPosition = _animator.GetIKPosition(AvatarIKGoal.RightFoot);

        // Get feet position
        currentFeatureVector[0] = leftFootPosition.x;
        currentFeatureVector[1] = leftFootPosition.y;
        currentFeatureVector[2] = leftFootPosition.z;

        currentFeatureVector[3] = rightFootPosition.x;
        currentFeatureVector[4] = rightFootPosition.y;
        currentFeatureVector[5] = rightFootPosition.z;

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
        currentFeatureVector[15] = _animator.velocity.x;
        currentFeatureVector[16] = _animator.velocity.y;
        currentFeatureVector[17] = _animator.velocity.z;

        // TODO: Obtain trajectory velocities
        currentFeatureVector[21] = _animator.velocity.x;
        currentFeatureVector[22] = _animator.velocity.y;
        currentFeatureVector[23] = _animator.velocity.z;
        return currentFeatureVector;
    }
}