using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary> </summary>
public class MatchingFeaturesDatabase
{
    private List<AnimationClip> _animationClips;

    private List<(string name, int frame, float[] featureVector)> _database;

    private string _databaseFilePath;
    private string _databaseText;

    // TODO: Have C++ handle calls to Tensorflow models
    [DllImport("MotionMatchingUnityPlugin", CallingConvention = CallingConvention.Cdecl)]
    private static extern int PrintANumber();

    [DllImport("MotionMatchingUnityPlugin", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr PrintHello();

    [DllImport("MotionMatchingUnityPlugin", CallingConvention = CallingConvention.Cdecl)]
    private static extern int AddTwoIntegers(int i1, int i2);

    [DllImport("MotionMatchingUnityPlugin", CallingConvention = CallingConvention.Cdecl)]
    private static extern float AddTwoFloats(float f1, float f2);

    public MatchingFeaturesDatabase(string databaseFilePath)
    {
        _database = new List<(string, int, float[])>();
        _animationClips = new List<AnimationClip>();
        _databaseFilePath = databaseFilePath;

        if (!File.Exists(_databaseFilePath))
        {
            Debug.LogError("Unable to find database file: " + _databaseFilePath);
        }

        Debug.Log("Parsing database file: " + _databaseFilePath);
        foreach (string line in File.ReadLines(_databaseFilePath))
        {
            string[] featureVectorStrings = line.Split(',');
            string name = featureVectorStrings[0];
            int.TryParse(featureVectorStrings[1], out int frame);
            string[] featureVectorSection = featureVectorStrings.SubArray(2, 27);
            float[] featureVector = featureVectorSection.Select(f =>
            {
                if (float.TryParse(f, out float number))
                {
                    return number;
                }
                return float.MinValue;
            }).ToArray();
            AddToDatabase(name, frame, featureVector);
        }

    }

    public void AddToDatabase(string name, int frame, float[] featureVector)
    {
        _database.Add((name, frame, featureVector));
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
        string sequenceName = _database[minIndex].name;
        int sequenceLength = _database.Count(d => d.name.ToLower().Equals(sequenceName)); // This might not scale in large databases
        int currentFrame = _database[minIndex].frame;
        return (sequenceName, (float)currentFrame / sequenceLength);
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
