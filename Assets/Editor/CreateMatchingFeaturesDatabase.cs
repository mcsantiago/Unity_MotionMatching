using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CreateMatchingFeaturesDatabase : ScriptableWizard
{
    [SerializeField]
    private string sourceDir = "Assets\\Resources\\lafan1";

    [MenuItem("Motion Matching/Create Matching Features Database From BVH")]
    static void MotionMatchingWizard()
    {
        ScriptableWizard.DisplayWizard<CreateMatchingFeaturesDatabase>("Create Motion Matching Animation Controller", "Create Database");
    }

    private void OnWizardCreate()
    {
        string output = EditorUtility.SaveFilePanel("New Matching Features Database", "Assets", "MatchingFeaturesDatabase", "sloth");
        string[] files = Directory.GetFiles(sourceDir);

        foreach (string file in files)
        {
            if (file.EndsWith(".bvh"))
            {
                FileInfo fi = new FileInfo(file);
                string bvhData = File.ReadAllText(file);
                BVHProcessor bp = new BVHProcessor(bvhData, fi.Name);
                WriteDatabase(output, bp.featureVectors);
            }
        }
    }

    private void WriteDatabase(string path, List<FeatureVector> featureVectors)
    {
        FileInfo fi = new FileInfo(path);
        using (StreamWriter sw = fi.AppendText())
        {
            foreach (FeatureVector featureVector in featureVectors)
            {
                sw.WriteLine(featureVector.ToString());
            }
        }
    }
}