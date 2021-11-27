using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class BVHProcessor
{
    public int frames = 0;
    public float frameTime = 1000f / 60f;
    public BVHBone root;
    public List<FeatureVector> featureVectors;
    public string name;
    public List<BVHBone> boneList;

    static private char[] charMap = null;
    private float[][] channels;
    private string bvhText;
    private int pos = 0;


    public override string ToString()
    {
        // public int frames = 0;
        // public float frametime = 1000f / 60f;
        // public bvhbone root;
        return "bvhprocessor:\n" +
        "frames: " + frames + "\n" +
        "frametime: " + frameTime + "\n" +
        "root: " + root.name;
    }

    private bool peek(out char c)
    {
        c = ' ';
        if (pos >= bvhText.Length)
        {
            return false;
        }
        c = bvhText[pos];
        return true;
    }

    private bool expect(string text)
    {
        foreach (char c in text)
        {
            if (pos >= bvhText.Length || (c != bvhText[pos] && bvhText[pos] < 256 && c != charMap[bvhText[pos]]))
            {
                return false;
            }
            pos++;
        }
        return true;
    }

    private bool getString(out string text)
    {
        text = "";
        while (pos < bvhText.Length && bvhText[pos] != '\n' && bvhText[pos] != '\r')
        {
            text += bvhText[pos++];
        }
        text = text.Trim();

        return (text.Length != 0);
    }

    private bool getChannel(out int channel)
    {
        channel = -1;
        if (pos + 1 >= bvhText.Length)
        {
            return false;
        }
        switch (bvhText[pos])
        {
            case 'x':
            case 'X':
                channel = 0;
                break;
            case 'y':
            case 'Y':
                channel = 1;
                break;
            case 'z':
            case 'Z':
                channel = 2;
                break;
            default:
                return false;
        }
        pos++;
        switch (bvhText[pos])
        {
            case 'p':
            case 'P':
                pos++;
                return expect("osition");
            case 'r':
            case 'R':
                pos++;
                channel += 3;
                return expect("otation");
            default:
                return false;
        }
    }

    private bool getInt(out int v)
    {
        bool negate = false;
        bool digitFound = false;
        v = 0;

        // Read sign
        if (pos < bvhText.Length && bvhText[pos] == '-')
        {
            negate = true;
            pos++;
        }
        else if (pos < bvhText.Length && bvhText[pos] == '+')
        {
            pos++;
        }

        // Read digits
        while (pos < bvhText.Length && bvhText[pos] >= '0' && bvhText[pos] <= '9')
        {
            v = v * 10 + (int)(bvhText[pos++] - '0');
            digitFound = true;
        }

        // Finalize
        if (negate)
        {
            v *= -1;
        }
        if (!digitFound)
        {
            v = -1;
        }
        return digitFound;
    }

    // Accuracy looks okay
    private bool getFloat(out float v)
    {
        bool negate = false;
        bool digitFound = false;
        int i = 0;
        v = 0f;

        // Read sign
        if (pos < bvhText.Length && bvhText[pos] == '-')
        {
            negate = true;
            pos++;
        }
        else if (pos < bvhText.Length && bvhText[pos] == '+')
        {
            pos++;
        }

        // Read digits before decimal point
        while (pos < bvhText.Length && bvhText[pos] >= '0' && bvhText[pos] <= '9')
        {
            v = v * 10 + (float)(bvhText[pos++] - '0');
            digitFound = true;
        }

        // Read decimal point
        if (pos < bvhText.Length && (bvhText[pos] == '.' || bvhText[pos] == ','))
        {
            pos++;

            // Read digits after decimal
            float fac = 0.1f;
            while (pos < bvhText.Length && bvhText[pos] >= '0' && bvhText[pos] <= '9' && i < 128)
            {
                v += fac * (float)(bvhText[pos++] - '0');
                fac *= 0.1f;
                digitFound = true;
            }
        }

        // Finalize
        if (negate)
        {
            v *= -1f;
        }
        if (!digitFound)
        {
            v = float.NaN;
        }
        return digitFound;
    }

    private void skip()
    {
        while (pos < bvhText.Length && (bvhText[pos] == ' ' || bvhText[pos] == '\t' || bvhText[pos] == '\n' || bvhText[pos] == '\r'))
        {
            pos++;
        }
    }

    private void skipInLine()
    {
        while (pos < bvhText.Length && (bvhText[pos] == ' ' || bvhText[pos] == '\t'))
        {
            pos++;
        }
    }

    private void newline()
    {
        bool foundNewline = false;
        skipInLine();
        while (pos < bvhText.Length && (bvhText[pos] == '\n' || bvhText[pos] == '\r'))
        {
            foundNewline = true;
            pos++;
        }
        assure("newline", foundNewline);
    }

    private void assure(string what, bool result)
    {
        if (!result)
        {
            string errorRegion = "";
            for (int i = Math.Max(0, pos - 15); i < Math.Min(bvhText.Length, pos + 15); i++)
            {
                if (i == pos - 1)
                {
                    errorRegion += ">>>";
                }
                errorRegion += bvhText[i];
                if (i == pos + 1)
                {
                    errorRegion += "<<<";
                }
            }
            throw new ArgumentException("Failed to parse BVH data at position " + pos + ". Expected " + what + " around here: " + errorRegion);
        }
    }

    private void assureExpect(string text)
    {
        assure(text, expect(text));
    }

    private void parse(bool overrideFrameTime, float time)
    {
        // Prepare character table
        if (charMap == null)
        {
            charMap = new char[256];
            for (int i = 0; i < 256; i++)
            {
                if (i >= 'a' && i <= 'z')
                {
                    charMap[i] = (char)(i - 'a' + 'A');
                }
                else if (i == '\t' || i == '\n' || i == '\r')
                {
                    charMap[i] = ' ';
                }
                else
                {
                    charMap[i] = (char)i;
                }
            }
        }

        // Parse skeleton
        skip();
        assureExpect("HIERARCHY");

        boneList = new List<BVHBone>();
        root = CreateBone();

        // Parse meta data
        skip();
        assureExpect("MOTION");
        skip();
        assureExpect("FRAMES:");
        skip();
        assure("frame number", getInt(out frames));
        skip();
        assureExpect("FRAME TIME:");
        skip();
        assure("frame time", getFloat(out frameTime));

        if (overrideFrameTime)
        {
            frameTime = time;
        }

        // Prepare channels
        int totalChannels = 0;
        foreach (BVHBone bone in boneList)
        {
            totalChannels += bone.channelNumber;
        }
        int channel = 0;
        channels = new float[totalChannels][];
        foreach (BVHBone bone in boneList)
        {
            for (int i = 0; i < bone.channelNumber; i++)
            {
                channels[channel] = new float[frames];
                bone.channels[bone.channelOrder[i]].values = channels[channel++];
            }
        }

        BVHBone leftFoot = boneList.Where(b => b.name.ToLower().Equals("leftfoot")).FirstOrDefault();
        BVHBone rightFoot = boneList.Where(b => b.name.ToLower().Equals("rightfoot")).FirstOrDefault();
        BVHBone hip = boneList.Where(b => b.name.ToLower().Equals("hips")).FirstOrDefault();

        Vector3 prevLeftFootPosition = Vector3.zero;
        Vector3 prevRightFootPosition = Vector3.zero;
        Vector3 prevHipPosition = Vector3.zero;
        // Parse frames
        for (int i = 0; i < frames; i++)
        {
            newline();
            for (channel = 0; channel < totalChannels; channel++)
            {
                skipInLine();
                assure("channel value", getFloat(out channels[channel][i]));
            }
            // Derive the feature vector
            FeatureVector prevFeatureVector1 = null; // One frame back
            FeatureVector prevFeatureVector2 = null; // Two frames back
            if (featureVectors.Count > 0)
            {
                prevFeatureVector1 = featureVectors[featureVectors.Count - 1];
            }
            if (featureVectors.Count > 1)
            {
                prevFeatureVector2 = featureVectors[featureVectors.Count - 2];
            }
            FeatureVector featureVector = new FeatureVector(name);
            featureVector.LeftFootPosition = GetBonePosition(leftFoot, i);
            // Get Left Foot Velocity
            featureVector.LeftFootVelocity = (featureVector.LeftFootPosition - prevLeftFootPosition) / frameTime;
            // Get Right Foot Position
            featureVector.RightFootPosition = GetBonePosition(rightFoot, i);
            // Get Right Foot Velocity
            featureVector.RightFootVelocity = (featureVector.RightFootPosition - prevRightFootPosition) / frameTime;
            // Get Hip Velocity
            featureVector.HipPosition = new Vector3(hip.channels[0].values[i], hip.channels[1].values[i], hip.channels[2].values[i]);
            featureVector.HipVelocity = (featureVector.HipPosition - prevHipPosition) / frameTime;
            // Get Hip Position of next two frames
            if (prevFeatureVector1 != null)
            {
                prevFeatureVector1.HipPosition_1 = new Vector3(hip.channels[0].values[i], hip.channels[1].values[i], hip.channels[2].values[i]);
                prevFeatureVector1.HipDir_1 = prevFeatureVector1.HipPosition_1 - prevFeatureVector1.HipPosition;
            }
            if (prevFeatureVector2 != null)
            {
                prevFeatureVector2.HipPosition_2 = new Vector3(hip.channels[0].values[i], hip.channels[1].values[i], hip.channels[2].values[i]);
                // Get Hip Direction of next two frames
                prevFeatureVector2.HipDir_2 = prevFeatureVector2.HipPosition_2 - prevFeatureVector2.HipPosition_1;

            }

            prevLeftFootPosition = featureVector.LeftFootPosition;
            prevRightFootPosition = featureVector.RightFootPosition;
            prevHipPosition = featureVector.HipPosition;

            featureVectors.Add(featureVector);
        }
    }

    private BVHBone CreateBone(BVHBone parent = null)
    {
        BVHBone bone = new BVHBone(parent);
        skip();
        if (parent == null)
        {
            assureExpect("ROOT");
        }
        else
        {
            assureExpect("JOINT");
        }
        assure("joint name", getString(out bone.name));
        skip();
        assureExpect("{");
        skip();
        assureExpect("OFFSET");
        skip();
        assure("offset X", getFloat(out bone.offsetX));
        skip();
        assure("offset Y", getFloat(out bone.offsetY));
        skip();
        assure("offset Z", getFloat(out bone.offsetZ));
        skip();
        assureExpect("CHANNELS");

        skip();
        assure("channel number", getInt(out bone.channelNumber));
        assure("valid channel number", bone.channelNumber >= 1 && bone.channelNumber <= 6);

        for (int i = 0; i < bone.channelNumber; i++)
        {
            skip();
            int channelId;
            assure("channel ID", getChannel(out channelId));
            bone.channelOrder[i] = channelId;
            bone.channels[channelId].enabled = true;
        }

        char peek = ' ';
        do
        {
            float ignored;
            skip();
            assure("child joint", this.peek(out peek));
            switch (peek)
            {
                case 'J':
                    BVHBone child = CreateBone(bone);
                    bone.children.Add(child);
                    break;
                case 'E':
                    assureExpect("End Site");
                    skip();
                    assureExpect("{");
                    skip();
                    assureExpect("OFFSET");
                    skip();
                    assure("end site offset X", getFloat(out ignored));
                    skip();
                    assure("end site offset Y", getFloat(out ignored));
                    skip();
                    assure("end site offset Z", getFloat(out ignored));
                    skip();
                    assureExpect("}");
                    break;
                case '}':
                    assureExpect("}");
                    break;
                default:
                    assure("child joint", false);
                    break;
            }
        } while (peek != '}');
        boneList.Add(bone);
        return bone;
    }

    private Vector3 GetBonePosition(BVHBone bone, int frame)
    {
        Vector3 position = new Vector3();
        Matrix4x4 rotateZ = Matrix4x4.Rotate(Quaternion.Euler(0, 0, bone.channels[5].values[frame]));
        Matrix4x4 rotateY = Matrix4x4.Rotate(Quaternion.Euler(0, bone.channels[4].values[frame], 0));
        Matrix4x4 rotateX = Matrix4x4.Rotate(Quaternion.Euler(bone.channels[3].values[frame], 0, 0));
        Matrix4x4 offset = Matrix4x4.Translate(new Vector3(bone.offsetX, bone.offsetY, bone.offsetZ));
        Matrix4x4 model = rotateZ * rotateY * rotateX * offset;
        position = model.MultiplyPoint3x4(position);
        BVHBone parent = bone.parent;
        while (parent != null)
        {
            Matrix4x4 parentRotateZ = Matrix4x4.Rotate(Quaternion.Euler(0, 0, parent.channels[5].values[frame]));
            Matrix4x4 parentRotateY = Matrix4x4.Rotate(Quaternion.Euler(0, parent.channels[4].values[frame], 0));
            Matrix4x4 parentRotateX = Matrix4x4.Rotate(Quaternion.Euler(parent.channels[3].values[frame], 0, 0));
            Matrix4x4 parentOffset = Matrix4x4.Translate(new Vector3(parent.offsetX, parent.offsetY, parent.offsetZ));
            Matrix4x4 parentModel = parentRotateZ * parentRotateY * parentRotateX * parentOffset;
            position = parentModel.MultiplyPoint3x4(position);
            parent = parent.parent;
        }
        return position;
    }

    public BVHProcessor(string bvhText, string name)
    {
        this.bvhText = bvhText;
        this.name = name;
        this.featureVectors = new List<FeatureVector>();

        parse(false, 0f);
    }

    public BVHProcessor(string bvhText, float time)
    {
        this.bvhText = bvhText;

        parse(true, time);
    }
}
