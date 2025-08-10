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
        if (_database == null || _database.Count == 0)
        {
            Debug.LogError("Feature database is empty.");
            return (string.Empty, 0f);
        }

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

        if (minIndex < 0)
        {
            return (string.Empty, 0f);
        }

        string sequenceName = _database[minIndex].name;
        // Count frames in the same sequence (case-insensitive)
        int sequenceLength = _database.Count(d => d.name.Equals(sequenceName, StringComparison.OrdinalIgnoreCase));
        int currentFrame = _database[minIndex].frame;

        // Normalize time to [0,1)
        float t = sequenceLength > 0 ? Mathf.Clamp01((float)currentFrame / Mathf.Max(1, sequenceLength - 1)) : 0f;

        Debug.Log($"Found sequence {sequenceName} frame {currentFrame}/{sequenceLength} dist {minDistance:F4}");
        return (sequenceName, t);
    }

    /// <summary>
    /// Returns the closest record including its name, frame index, feature vector, and distance.
    /// </summary>
    public (string name, int frame, float[] vector, float distance) GetClosestRecord(float[] query)
    {
        if (_database == null || _database.Count == 0)
        {
            return (string.Empty, 0, null, float.MaxValue);
        }

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

        if (minIndex < 0)
        {
            return (string.Empty, 0, null, float.MaxValue);
        }

        var rec = _database[minIndex];
        // return a copy of the vector to keep DB immutable outside
        float[] vec = new float[rec.featureVector.Length];
        Array.Copy(rec.featureVector, vec, rec.featureVector.Length);
        return (rec.name, rec.frame, vec, minDistance);
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
