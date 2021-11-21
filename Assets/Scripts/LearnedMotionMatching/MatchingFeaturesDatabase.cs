using System;
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

    private List<float[]> _features;

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
        // TODO: Load all bvh files
        TextAsset bvhFile = Resources.Load("lafan1/walk1_subject1.bvh") as TextAsset;
        Debug.Log(bvhFile.text);
        BVHParser bVHParser = new BVHParser(bvhFile.text);
        _features = new List<float[]>();
    }


    /// <summary> Gets the feature vector closest to the 
    /// query feature vector </summary>
    public float[] getClosestFeature(float[] query)
    {
        int minIndex = -1;
        float minDistance = float.MaxValue;
        for (int i = 0; i < _features.Count; i++)
        {
            float d = distance(query, _features[i]);
            if (d < minDistance)
            {
                minIndex = i;
                minDistance = d;
            }
        }
        return _features[minIndex];
    }

    /// <summary>Calculates the Euclidean distance between 
    /// two feature vectors</summary> 
    private float distance(float[] query, float[] target)
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
