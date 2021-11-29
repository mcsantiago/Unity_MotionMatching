using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BVHToAnimationClip : AssetPostprocessor
{
  private bool flexibleBoneNames = false;
  private Dictionary<string, string> renamingMap;
  static private int clipCount = 0;
  private BVHProcessor bp = null;
  private Transform rootBone;
  private string prefix;
  private int frames;
  private Dictionary<string, string> pathToBone;
  private Dictionary<string, string[]> boneToMuscles;
  private Dictionary<string, Transform> nameMap;

  void OnPreprocessAsset()
  {
    if (assetImporter.importSettingsMissing)
    {
      if (!assetPath.Contains(".bvh")) { return; }

      // TODO: BVH to AnimationClip logic here
      FileInfo fi = new FileInfo(assetPath);
      string bvhData = File.ReadAllText(assetPath);
      BVHProcessor bp = new BVHProcessor(bvhData, fi.Name);
      AnimationClip clip = new AnimationClip();

      foreach (BVHBone bone in bp.boneList)
      {
        for (int c = 0; c < bone.channels.Length; c++)
        {
          if (!bone.channels[c].enabled) continue;

          Keyframe[] keyframes = new Keyframe[bp.frames];
          float time = 0;
          for (int i = 0; i < bp.frames; i++)
          {
            keyframes[i] = new Keyframe(time, bone.channels[c].values[i]);
            time += bp.frameTime;
          }
          AnimationCurve curve = new AnimationCurve(keyframes);
          Debug.Log(bone.GetBonePath());
          clip.SetCurve(bone.GetBonePath(), typeof(Transform), GetChannelPropertyName(c), curve);
        }
      }
      AssetDatabase.CreateAsset(clip, "Assets/Resources/test.anim");
    }

  }

  public AnimationClip GetAnimationClip()
  {
    // if (bp == null)
    // {
    //   throw new InvalidOperationException("No BVH file has been parsed.");
    // }

    nameMap = new Dictionary<string, Transform>();
    renamingMap = new Dictionary<string, string>();

    // foreach (FakeDictionary entry in boneRenamingMap)
    // {
    //   if (entry.bvhName != "" && entry.targetName != "")
    //   {
    //     renamingMap.Add(flexibleName(entry.bvhName), flexibleName(entry.targetName));
    //   }
    // }

    Queue<Transform> transforms = new Queue<Transform>();
    transforms.Enqueue(targetAvatar.transform);
    string targetName = flexibleName(bp.root.name);
    if (renamingMap.ContainsKey(targetName))
    {
      targetName = flexibleName(renamingMap[targetName]);
    }
    while (transforms.Any())
    {
      Transform transform = transforms.Dequeue();
      if (flexibleName(transform.name) == targetName)
      {
        rootBone = transform;
        break;
      }
      if (nameMap.ContainsKey(targetName) && nameMap[targetName] == transform)
      {
        rootBone = transform;
        break;
      }
      for (int i = 0; i < transform.childCount; i++)
      {
        transforms.Enqueue(transform.GetChild(i));
      }
    }
    if (rootBone == null)
    {
      rootBone = BVHRecorder.getRootBone(targetAvatar);
      Debug.LogWarning("Using \"" + rootBone.name + "\" as the root bone.");
    }
    if (rootBone == null)
    {
      throw new InvalidOperationException("No root bone \"" + bp.root.name + "\" found.");
    }

    frames = bp.frames;
    clip = new AnimationClip();
    clip.name = "BVHClip (" + (clipCount++) + ")";
    if (clipName != "")
    {
      clip.name = clipName;
    }
    clip.legacy = true;
    prefix = getPathBetween(rootBone, targetAvatar.transform, true, true);

    Vector3 targetAvatarPosition = targetAvatar.transform.position;
    Quaternion targetAvatarRotation = targetAvatar.transform.rotation;
    targetAvatar.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
    targetAvatar.transform.rotation = Quaternion.identity;

    getCurves(prefix, bp.root, rootBone, true);

    targetAvatar.transform.position = targetAvatarPosition;
    targetAvatar.transform.rotation = targetAvatarRotation;

    clip.EnsureQuaternionContinuity();
    return clip;
  }

  private string flexibleName(string name)
  {
    if (!flexibleBoneNames)
    {
      return name;
    }
    name = name.Replace(" ", "");
    name = name.Replace("_", "");
    name = name.ToLower();
    return name;
  }

  private Transform getBoneByName(string name, Transform transform, bool first)
  {
    string targetName = flexibleName(name);
    if (renamingMap.ContainsKey(targetName))
    {
      targetName = flexibleName(renamingMap[targetName]);
    }
    if (first)
    {
      if (flexibleName(transform.name) == targetName)
      {
        return transform;
      }
      if (nameMap.ContainsKey(targetName) && nameMap[targetName] == transform)
      {
        return transform;
      }
    }
    for (int i = 0; i < transform.childCount; i++)
    {
      Transform child = transform.GetChild(i);
      if (flexibleName(child.name) == targetName)
      {
        return child;
      }
      if (nameMap.ContainsKey(targetName) && nameMap[targetName] == child)
      {
        return child;
      }
    }
    throw new InvalidOperationException("Could not find bone \"" + name + "\" under bone \"" + transform.name + "\".");
  }


  private void getCurves(string path, BVHBone node, Transform bone, bool first)
  {
    bool posX = false;
    bool posY = false;
    bool posZ = false;
    bool rotX = false;
    bool rotY = false;
    bool rotZ = false;

    float[][] values = new float[6][];
    Keyframe[][] keyframes = new Keyframe[7][];
    string[] props = new string[7];
    Transform nodeTransform = getBoneByName(node.name, bone, first);

    if (path != prefix)
    {
      path += "/";
    }
    if (rootBone != targetAvatar.transform || !first)
    {
      path += nodeTransform.name;
    }

    // This needs to be changed to gather from all channels into two vector3, invert the coordinate system transformation and then make keyframes from it
    for (int channel = 0; channel < 6; channel++)
    {
      if (!node.channels[channel].enabled)
      {
        continue;
      }

      switch (channel)
      {
        case 0:
          posX = true;
          props[channel] = "localPosition.x";
          break;
        case 1:
          posY = true;
          props[channel] = "localPosition.y";
          break;
        case 2:
          posZ = true;
          props[channel] = "localPosition.z";
          break;
        case 3:
          rotX = true;
          props[channel] = "localRotation.x";
          break;
        case 4:
          rotY = true;
          props[channel] = "localRotation.y";
          break;
        case 5:
          rotZ = true;
          props[channel] = "localRotation.z";
          break;
        default:
          channel = -1;
          break;
      }
      if (channel == -1)
      {
        continue;
      }

      keyframes[channel] = new Keyframe[frames];
      values[channel] = node.channels[channel].values;
      if (rotX && rotY && rotZ && keyframes[6] == null)
      {
        keyframes[6] = new Keyframe[frames];
        props[6] = "localRotation.w";
      }
    }

    float time = 0f;
    if (posX && posY && posZ)
    {
      Vector3 offset;
      if (blender)
      {
        offset = new Vector3(-node.offsetX, node.offsetZ, -node.offsetY);
      }
      else
      {
        offset = new Vector3(-node.offsetX, node.offsetY, node.offsetZ);
      }
      for (int i = 0; i < frames; i++)
      {
        time += 1f / frameRate;
        keyframes[0][i].time = time;
        keyframes[1][i].time = time;
        keyframes[2][i].time = time;
        if (blender)
        {
          keyframes[0][i].value = -values[0][i];
          keyframes[1][i].value = values[2][i];
          keyframes[2][i].value = -values[1][i];
        }
        else
        {
          keyframes[0][i].value = -values[0][i];
          keyframes[1][i].value = values[1][i];
          keyframes[2][i].value = values[2][i];
        }
        if (first)
        {
          Vector3 bvhPosition = bone.transform.parent.InverseTransformPoint(new Vector3(keyframes[0][i].value, keyframes[1][i].value, keyframes[2][i].value) + targetAvatar.transform.position + offset);
          keyframes[0][i].value = bvhPosition.x * targetAvatar.transform.localScale.x;
          keyframes[1][i].value = bvhPosition.y * targetAvatar.transform.localScale.y;
          keyframes[2][i].value = bvhPosition.z * targetAvatar.transform.localScale.z;
        }
      }
      if (first)
      {
        clip.SetCurve(path, typeof(Transform), props[0], new AnimationCurve(keyframes[0]));
        clip.SetCurve(path, typeof(Transform), props[1], new AnimationCurve(keyframes[1]));
        clip.SetCurve(path, typeof(Transform), props[2], new AnimationCurve(keyframes[2]));
      }
      else
      {
        Debug.LogWarning("Position information on bones other than the root bone is currently not supported and has been ignored. If you exported this file from Blender, please tick the \"Root Translation Only\" option next time.");
      }
    }

    time = 0f;
    if (rotX && rotY && rotZ)
    {
      Quaternion oldRotation = bone.transform.rotation;
      for (int i = 0; i < frames; i++)
      {
        Vector3 eulerBVH = new Vector3(wrapAngle(values[3][i]), wrapAngle(values[4][i]), wrapAngle(values[5][i]));
        Quaternion rot = fromEulerZXY(eulerBVH);
        if (blender)
        {
          keyframes[3][i].value = rot.x;
          keyframes[4][i].value = -rot.z;
          keyframes[5][i].value = rot.y;
          keyframes[6][i].value = rot.w;
          //rot2 = new Quaternion(rot.x, -rot.z, rot.y, rot.w);
        }
        else
        {
          keyframes[3][i].value = rot.x;
          keyframes[4][i].value = -rot.y;
          keyframes[5][i].value = -rot.z;
          keyframes[6][i].value = rot.w;
          //rot2 = new Quaternion(rot.x, -rot.y, -rot.z, rot.w);
        }
        if (first)
        {
          bone.transform.rotation = new Quaternion(keyframes[3][i].value, keyframes[4][i].value, keyframes[5][i].value, keyframes[6][i].value);
          keyframes[3][i].value = bone.transform.localRotation.x;
          keyframes[4][i].value = bone.transform.localRotation.y;
          keyframes[5][i].value = bone.transform.localRotation.z;
          keyframes[6][i].value = bone.transform.localRotation.w;
        }
        /*Vector3 euler = rot2.eulerAngles;
        keyframes[3][i].value = wrapAngle(euler.x);
        keyframes[4][i].value = wrapAngle(euler.y);
        keyframes[5][i].value = wrapAngle(euler.z);*/

        time += 1f / frameRate;
        keyframes[3][i].time = time;
        keyframes[4][i].time = time;
        keyframes[5][i].time = time;
        keyframes[6][i].time = time;
      }
      bone.transform.rotation = oldRotation;
      clip.SetCurve(path, typeof(Transform), props[3], new AnimationCurve(keyframes[3]));
      clip.SetCurve(path, typeof(Transform), props[4], new AnimationCurve(keyframes[4]));
      clip.SetCurve(path, typeof(Transform), props[5], new AnimationCurve(keyframes[5]));
      clip.SetCurve(path, typeof(Transform), props[6], new AnimationCurve(keyframes[6]));
    }

    foreach (BVHParser.BVHBone child in node.children)
    {
      getCurves(path, child, nodeTransform, false);
    }
  }

  private string GetChannelPropertyName(int ch)
  {
    switch (ch)
    {
      case 0: return "localPosition.x";
      case 1: return "localPosition.y";
      case 2: return "localPosition.z";
      case 3: return "localRotation.x";
      case 4: return "localRotation.y";
      case 5: return "localRotation.z";
    }
    return "";
  }
}
