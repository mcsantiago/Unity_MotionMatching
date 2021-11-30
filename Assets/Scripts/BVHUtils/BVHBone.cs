using System;
using System.Collections.Generic;
using System.Linq;

public class BVHBone
{
    public string name;
    public List<BVHBone> children;
    public float offsetX, offsetY, offsetZ;
    public int[] channelOrder;
    public int channelNumber;
    public BVHChannel[] channels;
    public BVHBone parent;

    // 0 = Xpos, 1 = Ypos, 2 = Zpos, 3 = Xrot, 4 = Yrot, 5 = Zrot
    public struct BVHChannel
    {
        public bool enabled;
        public float[] values;
    }

    public BVHBone(BVHBone parent = null)
    {
        channels = new BVHChannel[6];
        // X Pos, Y Pos, Z Pos, Z Rot, Y Rot, X Rot
        channelOrder = new int[6] { 0, 1, 2, 5, 4, 3 };
        children = new List<BVHBone>();
        this.parent = parent;
    }

    public string GetBonePath()
    {
        string path = "";
        path += this.name;
        BVHBone parent = this.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}