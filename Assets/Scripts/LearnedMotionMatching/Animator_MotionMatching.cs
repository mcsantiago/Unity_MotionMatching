using System;
using UnityEngine;

[ExecuteAlways]
public class Animator_MotionMatching : MonoBehaviour
{
    private Animator _animator;
    [SerializeField] private string _databaseFilePath = "Assets/MatchingFeaturesDatabase.csv";
    [SerializeField] private Transform _hips = null;
    [SerializeField] private Transform _leftFoot = null;
    [SerializeField] private Transform _rightFoot = null;
    private Vector3 _prevLeftFootPos;
    private Vector3 _prevRightFootPos;
    private Vector3 _prevLeftFootLocalPos;
    private Vector3 _prevRightFootLocalPos;
    private Vector3 _leftFootVelocity;
    private Vector3 _rightFootVelocity;
    private Vector3 _leftFootVelocityLocal;
    private Vector3 _rightFootVelocityLocal;
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

    [Header("Debug Visualization")]
    [SerializeField] private bool _drawFeatureGizmos = true;
    [SerializeField] private float _gizmoScale = 0.3f;
    [SerializeField] private Color _leftColor = Color.cyan;
    [SerializeField] private Color _rightColor = Color.magenta;
    [SerializeField] private Color _hipColor = Color.yellow;
    [SerializeField] private Color _dbColor = new Color(0.2f, 1f, 0.2f, 1f);
    [SerializeField] private bool _drawNumericLabels = true;
    [Header("Trajectory Visualization")]
    [SerializeField] private bool _drawTrajectory = true;
    [SerializeField] private int _trajectorySteps = 10;
    [SerializeField] private float _trajectoryStepSeconds = 0.1f;
    [SerializeField] private int _pastTrailFrames = 120;
    [SerializeField] private Color _trajectoryPastColor = new Color(1.0f, 0.65f, 0.0f, 1f); // orange
    [SerializeField] private Color _trajectoryFutureColor = new Color(0.2f, 0.8f, 1f, 1f); // light blue
    private readonly System.Collections.Generic.List<Vector3> _pastHipPositions = new System.Collections.Generic.List<Vector3>();
    private bool _gizmoWarningsLogged = false;

    // Predicted hip features (local to hips)
    private Vector3 _hipPos1World;
    private Vector3 _hipPos2World;
    private Vector3 _hipDir1World;
    private Vector3 _hipDir2World;

    [Header("Smoothing / Blending")]
    [SerializeField] private bool _smoothSwitching = true;
    [SerializeField] private float _crossFadeDuration = 0.08f;
    [SerializeField] private float _retimeThreshold = 0.12f; // normalized time difference to retime same clip
    [SerializeField] private float _retimeCrossFadeDuration = 0.05f;
    [SerializeField] private float _minTransitionInterval = 0.10f; // seconds
    private float _lastTransitionTime = -999f;

    private void Start()
    {
        _animator = this.GetComponent<Animator>();
        _animator.speed = 1.0f;
        _thirdPersonMovement = this.GetComponentInParent<ThirdPersonMovementKB>();
        _database = new MatchingFeaturesDatabase(_databaseFilePath);

        // Initialize previous positions to avoid large initial velocities
        if (_leftFoot != null)
        {
            _prevLeftFootPos = _leftFoot.position;
            _prevLeftFootLocalPos = _hips != null ? _hips.InverseTransformPoint(_leftFoot.position) : _leftFoot.position;
        }
        if (_rightFoot != null)
        {
            _prevRightFootPos = _rightFoot.position;
            _prevRightFootLocalPos = _hips != null ? _hips.InverseTransformPoint(_rightFoot.position) : _rightFoot.position;
        }
        if (_hips != null)
        {
            _prevHipPosition = _hips.position;
            _pastHipPositions.Clear();
            _pastHipPositions.Add(_hips.position);
        }
    }

    private void OnValidate()
    {
        // Ensure sensible defaults when fields are newly added
        if (_gizmoScale < 0.05f) _gizmoScale = 1.5f;
        // If the component existed before the flag was added, default to true for visibility
        // (Unity serializes default false for new bool fields on existing components)
        if (!_drawFeatureGizmos) _drawFeatureGizmos = true;
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

    private void LateUpdate()
    {
        // Runtime debug lines visible in Scene and Game (with Gizmos toggle on)
        if (!_drawFeatureGizmos || _hips == null || _leftFoot == null || _rightFoot == null) return;
        float s = _gizmoScale;
        Vector3 leftLocal = _hips.InverseTransformPoint(_leftFoot.position);
        Vector3 rightLocal = _hips.InverseTransformPoint(_rightFoot.position);
        Vector3 leftVelLocal = _leftFootVelocityLocal;
        Vector3 rightVelLocal = _rightFootVelocityLocal;
        Vector3 hipVel = _hipVelocity;

        Vector3 leftWorld = _hips.TransformPoint(leftLocal);
        Vector3 rightWorld = _hips.TransformPoint(rightLocal);
        Debug.DrawLine(leftWorld, leftWorld + _hips.TransformDirection(leftVelLocal) * s, _leftColor);
        Debug.DrawLine(rightWorld, rightWorld + _hips.TransformDirection(rightVelLocal) * s, _rightColor);
        Debug.DrawLine(_hips.position, _hips.position + hipVel * s, _hipColor);

        // Predicted hip positions
        Debug.DrawLine(_hips.position, _hipPos1World, _hipColor);
        Debug.DrawLine(_hipPos1World, _hipPos2World, _hipColor);

        if (_drawTrajectory)
        {
            // Past trail
            for (int i = 1; i < _pastHipPositions.Count; i++)
            {
                Debug.DrawLine(_pastHipPositions[i - 1], _pastHipPositions[i], _trajectoryPastColor);
            }
            // Future prediction
            Vector3 prev = _hips.position;
            for (int i = 1; i <= Mathf.Max(0, _trajectorySteps); i++)
            {
                float tf = _trajectoryStepSeconds * i;
                Vector3 p = _hips.position + _hipVelocity * tf;
                Debug.DrawLine(prev, p, _trajectoryFutureColor);
                prev = p;
            }
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
            bool sameClip = animationName.Equals(_currentClip);
            bool significantRetime = sameClip && Mathf.Abs(t - _currentFrameTime) > _retimeThreshold;
            bool clipChanged = !sameClip;
            bool canTransition = (Time.time - _lastTransitionTime) > _minTransitionInterval;

            if (_smoothSwitching && canTransition && (clipChanged || significantRetime))
            {
                float duration = clipChanged ? _crossFadeDuration : _retimeCrossFadeDuration;
                _animator.CrossFadeInFixedTime("Base Layer." + animationName, duration, 0, t);
                _lastTransitionTime = Time.time;
            }
            else if (clipChanged || Math.Abs(t - _currentFrameTime) > 1e-3)
            {
                _animator.Play("Base Layer." + animationName, 0, t);
            }

            if (clipChanged || significantRetime)
            {
                Debug.Log($"Now playing {animationName} at {t:F3}");
                _currentClip = animationName;
                _currentFrameTime = t;
            }
        }
        _featureVectorString = PrintUtil.FormatFloatToString(featureVector);
    }

    private void SetFeatures(float dt)
    {
        float safeDt = Mathf.Max(dt, 1e-5f);

        _leftFootVelocity = (_leftFoot.position - _prevLeftFootPos) / safeDt; // world
        _rightFootVelocity = (_rightFoot.position - _prevRightFootPos) / safeDt; // world
        _hipVelocity = (_hips.position - _prevHipPosition) / safeDt;

        // Update prev world positions
        _prevLeftFootPos = _leftFoot.position;
        _prevRightFootPos = _rightFoot.position;
        _prevHipPosition = _hips.position;

        // Compute local positions and local velocities (match DB semantics)
        Vector3 leftLocal = _hips.InverseTransformPoint(_leftFoot.position);
        Vector3 rightLocal = _hips.InverseTransformPoint(_rightFoot.position);
        _leftFootVelocityLocal = (leftLocal - _prevLeftFootLocalPos) / safeDt;
        _rightFootVelocityLocal = (rightLocal - _prevRightFootLocalPos) / safeDt;
        _prevLeftFootLocalPos = leftLocal;
        _prevRightFootLocalPos = rightLocal;

        // Predict hip future positions in world space using constant-velocity model
        _hipPos1World = _hips.position + _hipVelocity * safeDt;
        _hipPos2World = _hips.position + _hipVelocity * (2f * safeDt);
        _hipDir1World = _hipPos1World - _hips.position;
        _hipDir2World = _hipPos2World - _hipPos1World;

    }

    private void OnGUI()
    {
        InitStyles();
        GUI.Box(new Rect(0, 0, 1000, 20), "Matthew Santiago (mxs123530@utdallas.edu)");
        GUI.Box(new Rect(0, 20, 1000, 20), "CS 6323 - Computer Animation and Gaming Demo");
        GUI.Box(new Rect(0, 40, 1000, 20), "FeatureVector: " + _featureVectorString);
        GUI.Box(new Rect(0, 60, 1000, 20), $"Gizmos: {_drawFeatureGizmos}, Hips: {(_hips!=null)}, LFoot: {(_leftFoot!=null)}, RFoot: {(_rightFoot!=null)}");
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
        Vector3 leftFootPosition = _hips.InverseTransformPoint(_leftFoot.position);
        Vector3 rightFootPosition = _hips.InverseTransformPoint(_rightFoot.position);

        currentFeatureVector[0] = leftFootPosition.x;
        currentFeatureVector[1] = leftFootPosition.y;
        currentFeatureVector[2] = leftFootPosition.z;

        currentFeatureVector[3] = _leftFootVelocityLocal.x;
        currentFeatureVector[4] = _leftFootVelocityLocal.y;
        currentFeatureVector[5] = _leftFootVelocityLocal.z;

        currentFeatureVector[6] = rightFootPosition.x;
        currentFeatureVector[7] = rightFootPosition.y;
        currentFeatureVector[8] = rightFootPosition.z;

        currentFeatureVector[9] = _rightFootVelocityLocal.x;
        currentFeatureVector[10] = _rightFootVelocityLocal.y;
        currentFeatureVector[11] = _rightFootVelocityLocal.z;

        // Hip velocity in world space to match DB
        currentFeatureVector[12] = _hipVelocity.x;
        currentFeatureVector[13] = _hipVelocity.y;
        currentFeatureVector[14] = _hipVelocity.z;

        // Use predicted hip local positions and directions to align with database semantics
        currentFeatureVector[15] = _hipPos1World.x;
        currentFeatureVector[16] = _hipPos1World.y;
        currentFeatureVector[17] = _hipPos1World.z;

        currentFeatureVector[18] = _hipPos2World.x;
        currentFeatureVector[19] = _hipPos2World.y;
        currentFeatureVector[20] = _hipPos2World.z;

        currentFeatureVector[21] = _hipDir1World.x;
        currentFeatureVector[22] = _hipDir1World.y;
        currentFeatureVector[23] = _hipDir1World.z;

        currentFeatureVector[24] = _hipDir2World.x;
        currentFeatureVector[25] = _hipDir2World.y;
        currentFeatureVector[26] = _hipDir2World.z;

        return currentFeatureVector;
    }

    private void OnDrawGizmos()
    {
        if (!_drawFeatureGizmos || _hips == null || _leftFoot == null || _rightFoot == null)
        {
            if (!_gizmoWarningsLogged)
            {
                if (!_drawFeatureGizmos) Debug.LogWarning("Animator_MotionMatching: _drawFeatureGizmos is disabled; enable it in the inspector to see visualizations.");
                if (_hips == null) Debug.LogWarning("Animator_MotionMatching: Assign Hips transform in the inspector.");
                if (_leftFoot == null) Debug.LogWarning("Animator_MotionMatching: Assign LeftFoot transform in the inspector.");
                if (_rightFoot == null) Debug.LogWarning("Animator_MotionMatching: Assign RightFoot transform in the inspector.");
                _gizmoWarningsLogged = true;
            }
            return;
        }

        // Compute current local features (without mutating state)
        Vector3 leftLocal = _hips.InverseTransformPoint(_leftFoot.position);
        Vector3 rightLocal = _hips.InverseTransformPoint(_rightFoot.position);
        Vector3 leftVelLocal = _hips.InverseTransformDirection(_leftFootVelocity);
        Vector3 rightVelLocal = _hips.InverseTransformDirection(_rightFootVelocity);
        Vector3 hipVelLocal = _hips.InverseTransformDirection(_hipVelocity);

        Vector3 hipsWorld = _hips.position;

        // Left foot position and velocity
        Gizmos.color = _leftColor;
        Vector3 leftWorld = _hips.TransformPoint(leftLocal);
        Gizmos.DrawWireSphere(leftWorld, 0.03f * _gizmoScale);
        Gizmos.DrawLine(leftWorld, leftWorld + _hips.TransformDirection(leftVelLocal) * _gizmoScale);

        // Right foot position and velocity
        Gizmos.color = _rightColor;
        Vector3 rightWorld = _hips.TransformPoint(rightLocal);
        Gizmos.DrawWireSphere(rightWorld, 0.03f * _gizmoScale);
        Gizmos.DrawLine(rightWorld, rightWorld + _hips.TransformDirection(rightVelLocal) * _gizmoScale);

        // Hip velocity
        Gizmos.color = _hipColor;
        Gizmos.DrawLine(hipsWorld, hipsWorld + _hips.TransformDirection(hipVelLocal) * _gizmoScale);

        // Predicted hip positions (local) as points and directions
        Gizmos.DrawWireSphere(_hipPos1World, 0.025f * _gizmoScale);
        Gizmos.DrawWireSphere(_hipPos2World, 0.025f * _gizmoScale);
        Gizmos.DrawLine(hipsWorld, _hipPos1World);
        Gizmos.DrawLine(_hipPos1World, _hipPos2World);

        // Trajectory visualization
        if (_drawTrajectory)
        {
            // Past trail
            Gizmos.color = _trajectoryPastColor;
            for (int i = 1; i < _pastHipPositions.Count; i++)
            {
                Gizmos.DrawLine(_pastHipPositions[i - 1], _pastHipPositions[i]);
            }
            // Future points
            Gizmos.color = _trajectoryFutureColor;
            Vector3 prev = hipsWorld;
            for (int i = 1; i <= Mathf.Max(0, _trajectorySteps); i++)
            {
                float t = _trajectoryStepSeconds * i;
                Vector3 p = hipsWorld + _hipVelocity * t;
                Gizmos.DrawWireSphere(p, 0.02f * _gizmoScale);
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }

        // Draw the closest database feature vector for visual comparison if available
        if (Application.isPlaying && _database != null)
        {
            float[] cur = GetCurrentFeatureVector();
            var rec = _database.GetClosestRecord(cur);
            if (rec.vector != null && rec.vector.Length >= 27)
            {
                Gizmos.color = _dbColor;
                // DB foot positions are hips-local for feet, so transform to world for drawing
                Vector3 dbLeftLocal = new Vector3(rec.vector[0], rec.vector[1], rec.vector[2]);
                Vector3 dbRightLocal = new Vector3(rec.vector[6], rec.vector[7], rec.vector[8]);
                Vector3 dbLeftWorld = _hips.TransformPoint(dbLeftLocal);
                Vector3 dbRightWorld = _hips.TransformPoint(dbRightLocal);
                Gizmos.DrawWireSphere(dbLeftWorld, 0.02f * _gizmoScale);
                Gizmos.DrawWireSphere(dbRightWorld, 0.02f * _gizmoScale);

                // DB feet velocities are hips-local; draw as rays from positions
                Vector3 dbLeftVelLocal = new Vector3(rec.vector[3], rec.vector[4], rec.vector[5]);
                Vector3 dbRightVelLocal = new Vector3(rec.vector[9], rec.vector[10], rec.vector[11]);
                Gizmos.DrawLine(dbLeftWorld, dbLeftWorld + _hips.TransformDirection(dbLeftVelLocal) * _gizmoScale * 0.8f);
                Gizmos.DrawLine(dbRightWorld, dbRightWorld + _hips.TransformDirection(dbRightVelLocal) * _gizmoScale * 0.8f);

                // DB hip future positions are in world (frame 1 and 2)
                Vector3 dbHipPos1World = new Vector3(rec.vector[15], rec.vector[16], rec.vector[17]);
                Vector3 dbHipPos2World = new Vector3(rec.vector[18], rec.vector[19], rec.vector[20]);
                Gizmos.DrawWireCube(dbHipPos1World, Vector3.one * (0.02f * _gizmoScale));
                Gizmos.DrawWireCube(dbHipPos2World, Vector3.one * (0.02f * _gizmoScale));
                Gizmos.DrawLine(hipsWorld, dbHipPos1World);
                Gizmos.DrawLine(dbHipPos1World, dbHipPos2World);
            }
        }

#if UNITY_EDITOR
        if (_drawNumericLabels)
        {
            // Draw simple numeric labels near gizmos
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(leftWorld + Vector3.up * 0.02f, $"L {leftLocal.x:F2},{leftLocal.y:F2},{leftLocal.z:F2}\nLv {leftVelLocal.x:F2},{leftVelLocal.y:F2},{leftVelLocal.z:F2}");
            UnityEditor.Handles.Label(rightWorld + Vector3.up * 0.02f, $"R {rightLocal.x:F2},{rightLocal.y:F2},{rightLocal.z:F2}\nRv {rightVelLocal.x:F2},{rightVelLocal.y:F2},{rightVelLocal.z:F2}");
            UnityEditor.Handles.Label(hipsWorld + Vector3.up * 0.05f, $"Hv {_hipVelocity.x:F2},{_hipVelocity.y:F2},{_hipVelocity.z:F2}");
        }
#endif
    }

    private void OnDrawGizmosSelected()
    {
        // Draw same gizmos when the GameObject is selected
        OnDrawGizmos();
    }
}