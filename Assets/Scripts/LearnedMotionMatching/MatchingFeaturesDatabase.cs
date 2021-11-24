using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary> </summary>
public class MatchingFeaturesDatabase : MonoBehaviour
{
    private static MatchingFeaturesDatabase _instance;
    public static MatchingFeaturesDatabase Instance
    {
        get { return _instance; }
    }

    private List<(string name, float[] featureVector)> _database;

    [SerializeField] private string _databaseFile;

    public Boolean IsLoaded { get { return _isLoaded; } set { _isLoaded = value; } }

    // TODO: Have C++ handle calls to Tensorflow models
    [DllImport("MotionMatchingUnityPlugin", CallingConvention = CallingConvention.Cdecl)]
    private static extern int PrintANumber();

    [DllImport("MotionMatchingUnityPlugin", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr PrintHello();

    [DllImport("MotionMatchingUnityPlugin", CallingConvention = CallingConvention.Cdecl)]
    private static extern int AddTwoIntegers(int i1, int i2);

    [DllImport("MotionMatchingUnityPlugin", CallingConvention = CallingConvention.Cdecl)]
    private static extern float AddTwoFloats(float f1, float f2);

    private void Awake()
    {
        // This class is intended for use as a Singleton
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    private void Start()
    {
        _database = new List<(string, float[])>();
    }

    public void AddToDatabase(string name, float[] featureVector)
    {
        _database.Add((name, featureVector));
    }

    public void Print()
    {
        for (int i = 0; i < _database.Count; i++)
        {
            Debug.Log(_database[i].name + " " + PrintUtil.FormatFloatToString(_database[i].featureVector));
        }
    }

    /// <summary> Gets the feature vector closest to the 
    /// query feature vector </summary>
    public (string, float) GetClosestFeature(float[] query)
    {
        int minIndex = -1;
        float minDistance = float.MaxValue;
        for (int i = 0; i < _database.Count; i++)
        {
            float d = Distance(query, _database[i].featureVector);
            if (d < minDistance)
            {
                minIndex = i;
                minDistance = d;
            }
        }
        return ("Base Layer.Walking", 0.25f);
    }

    /// <summary>Calculates the Euclidean distance between 
    /// two feature vectors</summary> 
    private float Distance(float[] query, float[] target)
    {
        if (query.Length != target.Length)
        {
            throw new InvalidOperationException("Mismatching vector sizes");
        }

        float sumOfDifferences = 0;
        for (int i = 0; i < query.Length; i++)
        {
            sumOfDifferences += (float)Math.Pow((query[i] - target[i]), 2);
        }
        return (float)Math.Sqrt(sumOfDifferences);
    }
}
