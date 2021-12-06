using System;
using UnityEngine;

public class Animator_MotionMatching : MonoBehaviour
{
    private Animator _animator;
    [SerializeField] private string _databaseFilePath = "Assets/MatchingFeaturesDatabase.csv";
    [SerializeField] private Transform _hips = null;
    [SerializeField] private Transform _leftFoot = null;
    [SerializeField] private Transform _rightFoot = null;
    private Vector3 _prevLeftFootPos;
    private Vector3 _prevRightFootPos;
    private Vector3 _leftFootVelocity;
    private Vector3 _rightFootVelocity;
    private Vector3 _prevHipPosition;
    private Vector3 _hipVelocity;
    private Vector3 _trajectoryPos1;
    private Vector3 _trajectoryPos2;
    private Vector3 _trajectoryVel1;
    private Vector3 _trajectoryVel2;

    private MatchingFeaturesDatabase _database;
    private ThirdPersonMovementKB _thirdPersonMovement;

    private bool _isIdle = false;
    private string _currentClip = "";
    private float _currentFrameTime = 0;

    // DEBUG
    private string _featureVectorString;
    private GUIStyle _currentStyle = null;

    private void Start()
    {
        _animator = this.GetComponent<Animator>();
        _animator.speed = 1.0f;
        _thirdPersonMovement = this.GetComponentInParent<ThirdPersonMovementKB>();
        _database = new MatchingFeaturesDatabase(_databaseFilePath);
    }

    private void Update()
    {
        if (_thirdPersonMovement.MoveDirection2 == Vector3.zero && !_animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
        {
            _animator.Play("Idle", 0);
            _isIdle = true;
            _currentClip = "";
            _currentFrameTime = 0;
        }
        else if (_thirdPersonMovement.MoveDirection2 != Vector3.zero)
        {
            _isIdle = false;
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (_isIdle) return;

        // TODO: If the animation clip direction is different from controller direction, THEN find another section to play
        SetFeatures(Time.deltaTime);
        float[] featureVector = GetCurrentFeatureVector();
        if (_database != null)
        {
            (string animationName, float t) = _database.GetClosestFeature(featureVector);
            if (!animationName.Equals(_currentClip) || Math.Abs(t - _currentFrameTime) > 1e-3)
            {
                _animator.Play("Base Layer." + animationName, 0, t);
                Debug.Log("Now playing " + animationName + " at " + t);
                _currentClip = animationName;
                _currentFrameTime = t;
            }
        }
        _featureVectorString = PrintUtil.FormatFloatToString(featureVector);
    }

    private void SetFeatures(float dt)
    {
        _leftFootVelocity = (_leftFoot.position - _prevLeftFootPos) / dt;
        _rightFootVelocity = (_rightFoot.position - _prevRightFootPos) / dt;
        _hipVelocity = (_hips.position - _prevHipPosition) / dt;

        _prevLeftFootPos = _leftFoot.position;
        _prevRightFootPos = _rightFoot.position;
        _prevHipPosition = _hips.position;

        _trajectoryPos1 = _thirdPersonMovement.MoveDirection1;
        _trajectoryPos2 = _thirdPersonMovement.MoveDirection2;
        _trajectoryVel1 = _thirdPersonMovement.MoveVelocity1;
        _trajectoryVel2 = _thirdPersonMovement.MoveVelocity2;

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
        Vector3 leftFootPosition = _leftFoot.position - _leftFoot.root.position;
        Vector3 rightFootPosition = _rightFoot.position - _rightFoot.root.position;

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

        currentFeatureVector[15] = _trajectoryPos1.x;
        currentFeatureVector[16] = _trajectoryPos1.y;
        currentFeatureVector[17] = _trajectoryPos1.z;

        currentFeatureVector[18] = _trajectoryPos2.x;
        currentFeatureVector[19] = _trajectoryPos2.y;
        currentFeatureVector[20] = _trajectoryPos2.z;

        currentFeatureVector[21] = _trajectoryVel1.x;
        currentFeatureVector[22] = _trajectoryVel1.y;
        currentFeatureVector[23] = _trajectoryVel1.z;

        currentFeatureVector[24] = _trajectoryVel2.x;
        currentFeatureVector[25] = _trajectoryVel2.y;
        currentFeatureVector[26] = _trajectoryVel2.z;

        return currentFeatureVector;
    }
}