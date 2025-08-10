using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LearnedMotionMatching
{
    public class MotionData
    {
        public Vector3 HipsPosition { get; set; }
        public Vector3 HipsRotation { get; set; }
        
        public Vector3 LeftUpLegPosition { get; set; }
        public Vector3 LeftUpLegRotation { get; set; }
        public Vector3 LeftLegPosition { get; set; }
        public Vector3 LeftLegRotation { get; set; }
        public Vector3 LeftFootPosition { get; set; }
        public Vector3 LeftFootRotation { get; set; }
        public Vector3 LeftToePosition { get; set; }
        public Vector3 LeftToeRotation { get; set; }
        
        public Vector3 RightUpLegPosition { get; set; }
        public Vector3 RightUpLegRotation { get; set; }
        public Vector3 RightLegPosition { get; set; }
        public Vector3 RightLegRotation { get; set; }
        public Vector3 RightFootPosition { get; set; }
        public Vector3 RightFootRotation { get; set; }
        public Vector3 RightToePosition { get; set; }
        public Vector3 RightToeRotation { get; set; }
        
        public Vector3 SpinePosition { get; set; }
        public Vector3 SpineRotation { get; set; }
        public Vector3 Spine1Position { get; set; }
        public Vector3 Spine1Rotation { get; set; }
        public Vector3 Spine2Position { get; set; }
        public Vector3 Spine2Rotation { get; set; }
        
        public Vector3 LeftShoulderPosition { get; set; }
        public Vector3 LeftShoulderRotation { get; set; }
        public Vector3 LeftArmPosition { get; set; }
        public Vector3 LeftArmRotation { get; set; }
        public Vector3 LeftForeArmPosition { get; set; }
        public Vector3 LeftForeArmRotation { get; set; }
        public Vector3 LeftHandPosition { get; set; }
        public Vector3 LeftHandRotation { get; set; }
        
        public Vector3 NeckPosition { get; set; }
        public Vector3 NeckRotation { get; set; }
        public Vector3 HeadPosition { get; set; }
        public Vector3 HeadRotation { get; set; }
        
        public Vector3 RightShoulderPosition { get; set; }
        public Vector3 RightShoulderRotation { get; set; }
        public Vector3 RightArmPosition { get; set; }
        public Vector3 RightArmRotation { get; set; }
        public Vector3 RightForeArmPosition { get; set; }
        public Vector3 RightForeArmRotation { get; set; }
        public Vector3 RightHandPosition { get; set; }
        public Vector3 RightHandRotation { get; set; }

        public override string ToString()
        {
            // Implement this 
            return base.ToString();
        }

        public static float Distance(MotionData a, MotionData b)
        {
            return 0.0f;
        }
    }

    /// <summary>
    /// Represents a database for matching features based on animation clips.
    /// </summary>
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
            int sequenceLength = _database.Count(d => d.name.Equals(sequenceName, StringComparison.OrdinalIgnoreCase));
            int currentFrame = _database[minIndex].frame;

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
}