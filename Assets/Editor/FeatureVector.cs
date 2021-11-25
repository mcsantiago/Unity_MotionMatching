using System;
using UnityEngine;

public class FeatureVector
{
    public string Name { get; set; }
    public Vector3 LeftFootPosition { get; set; }
    public Vector3 LeftFootVelocity { get; set; }
    public Vector3 RightFootPosition { get; set; }
    public Vector3 RightFootVelocity { get; set; }
    public Vector3 HipPosition { get; set; }
    public Vector3 HipVelocity { get; set; }
    public Vector3 HipPosition_1 { get; set; }
    public Vector3 HipPosition_2 { get; set; }
    public Vector3 HipDir_1 { get; set; }
    public Vector3 HipDir_2 { get; set; }

    public FeatureVector(string name)
    {
        Name = name;
    }

    public override string ToString()
    {
        return Name + ", " + LeftFootPosition.x + ", " + LeftFootPosition.y + ", " + LeftFootPosition.z + ", " +
                LeftFootVelocity.x + ", " + LeftFootVelocity.y + ", " + LeftFootVelocity.z + ", " +
                RightFootPosition.x + ", " + RightFootPosition.y + ", " + RightFootPosition.z + ", " +
                RightFootVelocity.x + ", " + RightFootVelocity.y + ", " + RightFootVelocity.z + ", " +
                HipVelocity.x + ", " + HipVelocity.y + ", " + HipVelocity.z + ", " +
                HipPosition_1.x + ", " + HipPosition_1.y + ", " + HipPosition_1.z + ", " +
                HipPosition_2.x + ", " + HipPosition_2.y + ", " + HipPosition_2.z + ", " +
                HipDir_1.x + ", " + HipDir_1.y + ", " + HipDir_1.z + ", " +
                HipDir_2.x + ", " + HipDir_2.y + ", " + HipDir_2.z;
    }

}