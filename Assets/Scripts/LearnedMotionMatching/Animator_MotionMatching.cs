using System;
using UnityEngine;

public class Animator_MotionMatching : MonoBehaviour
{
    private Animator _animator;
    [SerializeField] private Avatar _avatar;
    [SerializeField] private string _databaseFilePath = "Assets/MatchingFeaturesDatabase.sloth";
    //   [SerializeField] private GameObject _hips = null;
    //   [SerializeField] private GameObject _leftFoot = null;
    //   [SerializeField] private GameObject _rightFoot = null;
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

    private int _currentClip = 0;
    private int _currentFrame = 0;

    // DEBUG
    private string _featureVectorString;
    private GUIStyle _currentStyle = null;

    private void Start()
    {
        _animator = this.GetComponent<Animator>();
        _database = new MatchingFeaturesDatabase(_databaseFilePath);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        SetFeatures(Time.deltaTime);
        float[] featureVector = GetCurrentFeatureVector();
        if (_database != null)
        {
            (string animationName, float t) = _database.GetClosestFeature(featureVector);
            _animator.Play(animationName, 0, t);
            Debug.Log("Now playing " + animationName + " at " + t);
        }
        _featureVectorString = PrintUtil.FormatFloatToString(featureVector);
    }

    private void SetFeatures(float dt)
    {
        Vector3 newLeftFootPosition = _animator.GetIKPosition(AvatarIKGoal.LeftFoot);
        Vector3 newRightFootPosition = _animator.GetIKPosition(AvatarIKGoal.RightFoot);
        Vector3 newHipPosition = _animator.rootPosition;

        // Velocities are small..we may have to do this for all the animations

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

        Vector3 leftFootPosition = _animator.GetIKPosition(AvatarIKGoal.LeftFoot);
        Vector3 rightFootPosition = _animator.GetIKPosition(AvatarIKGoal.RightFoot);

        // Get feet position
        currentFeatureVector[0] = leftFootPosition.x;
        currentFeatureVector[1] = leftFootPosition.y;
        currentFeatureVector[2] = leftFootPosition.z;

        currentFeatureVector[3] = _leftFootVelocity.x;
        currentFeatureVector[4] = _leftFootVelocity.y;
        currentFeatureVector[5] = _leftFootVelocity.z;

        currentFeatureVector[6] = rightFootPosition.x;
        currentFeatureVector[7] = rightFootPosition.y;
        currentFeatureVector[8] = rightFootPosition.z;

        currentFeatureVector[9] = _rightFootVelocity.x;
        currentFeatureVector[10] = _rightFootVelocity.y;
        currentFeatureVector[11] = _rightFootVelocity.z;

        currentFeatureVector[12] = _hipVelocity.x;
        currentFeatureVector[13] = _hipVelocity.y;
        currentFeatureVector[14] = _hipVelocity.z;

        // TODO: Obtain trajectory positions
        // TODO: Obtain trajectory velocities
        return currentFeatureVector;
    }
}