using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BVHProcessor
{
    public int Frames = 0;
    public float FrameTime = 1000f / 60f;
    public BVHBone Root;
    public List<FeatureVector> FeatureVectors;
    public string Name;
    public List<BVHBone> BoneList;

    static private char[] _charMap = null;
    private float[][] _channels;
    private string _bvhText;
    private int _pos = 0;
    private int _offsetStart = 0;
    private int _endFrame = -1;


    public override string ToString()
    {
        // public int frames = 0;
        // public float frametime = 1000f / 60f;
        // public bvhbone root;
        return "bvhprocessor:\n" +
        "frames: " + Frames + "\n" +
        "frametime: " + FrameTime + "\n" +
        "root: " + Root.name;
    }

    private bool peek(out char c)
    {
        c = ' ';
        if (_pos >= _bvhText.Length)
        {
            return false;
        }
        c = _bvhText[_pos];
        return true;
    }

    private bool expect(string text)
    {
        foreach (char c in text)
        {
            if (_pos >= _bvhText.Length || (c != _bvhText[_pos] && _bvhText[_pos] < 256 && c != _charMap[_bvhText[_pos]]))
            {
                return false;
            }
            _pos++;
        }
        return true;
    }

    private bool getString(out string text)
    {
        text = "";
        while (_pos < _bvhText.Length && _bvhText[_pos] != '\n' && _bvhText[_pos] != '\r')
        {
            text += _bvhText[_pos++];
        }
        text = text.Trim();

        return (text.Length != 0);
    }

    private bool getChannel(out int channel)
    {
        channel = -1;
        if (_pos + 1 >= _bvhText.Length)
        {
            return false;
        }
        switch (_bvhText[_pos])
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
        _pos++;
        switch (_bvhText[_pos])
        {
            case 'p':
            case 'P':
                _pos++;
                return expect("osition");
            case 'r':
            case 'R':
                _pos++;
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
        if (_pos < _bvhText.Length && _bvhText[_pos] == '-')
        {
            negate = true;
            _pos++;
        }
        else if (_pos < _bvhText.Length && _bvhText[_pos] == '+')
        {
            _pos++;
        }

        // Read digits
        while (_pos < _bvhText.Length && _bvhText[_pos] >= '0' && _bvhText[_pos] <= '9')
        {
            v = v * 10 + (int)(_bvhText[_pos++] - '0');
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
        if (_pos < _bvhText.Length && _bvhText[_pos] == '-')
        {
            negate = true;
            _pos++;
        }
        else if (_pos < _bvhText.Length && _bvhText[_pos] == '+')
        {
            _pos++;
        }

        // Read digits before decimal point
        while (_pos < _bvhText.Length && _bvhText[_pos] >= '0' && _bvhText[_pos] <= '9')
        {
            v = v * 10 + (float)(_bvhText[_pos++] - '0');
            digitFound = true;
        }

        // Read decimal point
        if (_pos < _bvhText.Length && (_bvhText[_pos] == '.' || _bvhText[_pos] == ','))
        {
            _pos++;

            // Read digits after decimal
            float fac = 0.1f;
            while (_pos < _bvhText.Length && _bvhText[_pos] >= '0' && _bvhText[_pos] <= '9' && i < 128)
            {
                v += fac * (float)(_bvhText[_pos++] - '0');
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
        while (_pos < _bvhText.Length && (_bvhText[_pos] == ' ' || _bvhText[_pos] == '\t' || _bvhText[_pos] == '\n' || _bvhText[_pos] == '\r'))
        {
            _pos++;
        }
    }

    private void skipInLine()
    {
        while (_pos < _bvhText.Length && (_bvhText[_pos] == ' ' || _bvhText[_pos] == '\t'))
        {
            _pos++;
        }
    }

    private void newline()
    {
        bool foundNewline = false;
        skipInLine();
        while (_pos < _bvhText.Length && (_bvhText[_pos] == '\n' || _bvhText[_pos] == '\r'))
        {
            foundNewline = true;
            _pos++;
        }
        assure("newline", foundNewline);
    }

    private void assure(string what, bool result)
    {
        if (!result)
        {
            string errorRegion = "";
            for (int i = Math.Max(0, _pos - 15); i < Math.Min(_bvhText.Length, _pos + 15); i++)
            {
                if (i == _pos - 1)
                {
                    errorRegion += ">>>";
                }
                errorRegion += _bvhText[i];
                if (i == _pos + 1)
                {
                    errorRegion += "<<<";
                }
            }
            throw new ArgumentException("Failed to parse BVH data at position " + _pos + ". Expected " + what + " around here: " + errorRegion);
        }
    }

    private void assureExpect(string text)
    {
        assure(text, expect(text));
    }

    private void parse(bool overrideFrameTime, float time)
    {
        // Prepare character table
        if (_charMap == null)
        {
            _charMap = new char[256];
            for (int i = 0; i < 256; i++)
            {
                if (i >= 'a' && i <= 'z')
                {
                    _charMap[i] = (char)(i - 'a' + 'A');
                }
                else if (i == '\t' || i == '\n' || i == '\r')
                {
                    _charMap[i] = ' ';
                }
                else
                {
                    _charMap[i] = (char)i;
                }
            }
        }

        // Parse skeleton
        skip();
        assureExpect("HIERARCHY");

        BoneList = new List<BVHBone>();
        Root = CreateBone();

        // Parse meta data
        skip();
        assureExpect("MOTION");
        skip();
        assureExpect("FRAMES:");
        skip();
        assure("frame number", getInt(out Frames));
        skip();
        assureExpect("FRAME TIME:");
        skip();
        assure("frame time", getFloat(out FrameTime));

        if (overrideFrameTime)
        {
            FrameTime = time;
        }

        // Prepare channels
        int totalChannels = 0;
        foreach (BVHBone bone in BoneList)
        {
            totalChannels += bone.channelNumber;
        }
        int channel = 0;
        _channels = new float[totalChannels][];
        foreach (BVHBone bone in BoneList)
        {
            for (int i = 0; i < bone.channelNumber; i++)
            {
                _channels[channel] = new float[Frames];
                bone.channels[bone.channelOrder[i]].values = _channels[channel++];
            }
        }
        
        BVHBone leftFoot = BoneList.Where(b => b.name.ToLower().Equals("leftfoot")).FirstOrDefault();
        BVHBone rightFoot = BoneList.Where(b => b.name.ToLower().Equals("rightfoot")).FirstOrDefault();
        BVHBone hip = BoneList.Where(b => b.name.ToLower().Equals("hips")).FirstOrDefault();

        Vector3 prevLeftFootPosition = Vector3.zero;
        Vector3 prevRightFootPosition = Vector3.zero;
        Vector3 prevHipPosition = Vector3.zero;

        int frames = _endFrame > -1 ? Math.Min(_endFrame, Frames) : Frames;
        // Parse frames
        for (int i = _offsetStart; i < frames; i++)
        {
            newline();
            for (channel = 0; channel < totalChannels; channel++)
            {
                skipInLine();
                assure("channel value", getFloat(out _channels[channel][i]));
            }
            // Derive the feature vector
            FeatureVector prevFeatureVector1 = null; // One frame back
            FeatureVector prevFeatureVector2 = null; // Two frames back
            if (FeatureVectors.Count > 0)
            {
                prevFeatureVector1 = FeatureVectors[FeatureVectors.Count - 1];
            }
            if (FeatureVectors.Count > 1)
            {
                prevFeatureVector2 = FeatureVectors[FeatureVectors.Count - 2];
            }
            FeatureVector featureVector = new FeatureVector(Name);
            featureVector.Frame = i - _offsetStart;
            featureVector.LeftFootPosition = GetBonePosition(leftFoot, i);
            // Get Left Foot Velocity
            featureVector.LeftFootVelocity = (featureVector.LeftFootPosition - prevLeftFootPosition) / FrameTime;
            // Get Right Foot Position
            featureVector.RightFootPosition = GetBonePosition(rightFoot, i);
            // Get Right Foot Velocity
            featureVector.RightFootVelocity = (featureVector.RightFootPosition - prevRightFootPosition) / FrameTime;
            // Get Hip Velocity
            featureVector.HipPosition = new Vector3(hip.channels[0].values[i], hip.channels[1].values[i], hip.channels[2].values[i]);
            featureVector.HipVelocity = (featureVector.HipPosition - prevHipPosition) / FrameTime;
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

            FeatureVectors.Add(featureVector);
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
        BoneList.Add(bone);
        return bone;
    }

    private Vector3 GetBonePosition(BVHBone bone, int frame)
    {
        Vector3 position = new Vector3();
        // Note: BVH convention is typically ZXY or XYZ; keep existing but guard for missing rotation channels
        Vector3 rotateZVector = new Vector3(0, 0, bone.channels[5].enabled ? bone.channels[5].values[frame] : 0f);
        Vector3 rotateYVector = new Vector3(0, bone.channels[4].enabled ? bone.channels[4].values[frame] : 0f, 0);
        Vector3 rotateXVector = new Vector3(bone.channels[3].enabled ? bone.channels[3].values[frame] : 0f, 0, 0);
        Matrix4x4 rotateZ = Matrix4x4.Rotate(Quaternion.Euler(rotateZVector));
        Matrix4x4 rotateY = Matrix4x4.Rotate(Quaternion.Euler(rotateYVector));
        Matrix4x4 rotateX = Matrix4x4.Rotate(Quaternion.Euler(rotateXVector));
        Matrix4x4 offset = Matrix4x4.Translate(new Vector3(bone.offsetX, bone.offsetY, bone.offsetZ));
        Matrix4x4 model = rotateZ * rotateY * rotateX * offset;
        position = model.MultiplyPoint3x4(position);
        BVHBone parent = bone.parent;
        while (parent != null)
        {
            rotateZVector = new Vector3(0, 0, parent.channels[5].enabled ? parent.channels[5].values[frame] : 0f);
            rotateYVector = new Vector3(0, parent.channels[4].enabled ? parent.channels[4].values[frame] : 0f, 0);
            rotateXVector = new Vector3(parent.channels[3].enabled ? parent.channels[3].values[frame] : 0f, 0, 0);
            rotateZ = Matrix4x4.Rotate(Quaternion.Euler(rotateZVector));
            rotateY = Matrix4x4.Rotate(Quaternion.Euler(rotateYVector));
            rotateX = Matrix4x4.Rotate(Quaternion.Euler(rotateXVector));
            // Ignore hip position
            offset = !parent.name.Equals("Hips") ? Matrix4x4.Translate(new Vector3(parent.offsetX, parent.offsetY, parent.offsetZ)) : Matrix4x4.identity;
            model = rotateZ * rotateY * rotateX * offset;
            position = model.MultiplyPoint3x4(position);
            parent = parent.parent;
        }
        return position;
    }

    public BVHProcessor(string bvhText, string name, int offsetStart, int endFrame)
    {
        _bvhText = bvhText;
        _offsetStart = offsetStart;
        _endFrame = endFrame;
        Name = name;
        FeatureVectors = new List<FeatureVector>();

        parse(false, 0f);
    }

    public BVHProcessor(string bvhText, float time)
    {
        _bvhText = bvhText;

        parse(true, time);
    }
}
