// ConstructFakeArm Unity Editor Script
// This file is licensed under the MIT License.
// Copyright (c) 2020 Lyuma <xn.lyuma@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
public class ConstructFakeArm : EditorWindow {

    [MenuItem ("CONTEXT/SkinnedMeshRenderer/Construct Fake Arm [Left] : LMT", false, 131)]
    public static void DoConstructFakeArmLeft (MenuCommand command)
    {
        DoConstructFakeArm(command, false);
    }

    [MenuItem ("CONTEXT/SkinnedMeshRenderer/Construct Fake Arm [Right] : LMT", false, 131)]
    public static void DoConstructFakeArmRight (MenuCommand command)
    {
        DoConstructFakeArm(command, true);
    }

    static float sumWeight(BoneWeight bw, HashSet<int> armBones) {
        float weight = 0;
        weight += (armBones.Contains(bw.boneIndex0) ? bw.weight0 : 0);
        weight += (armBones.Contains(bw.boneIndex1) ? bw.weight1 : 0);
        weight += (armBones.Contains(bw.boneIndex2) ? bw.weight2 : 0);
        weight += (armBones.Contains(bw.boneIndex3) ? bw.weight3 : 0);
        return weight;
    }
    static float sumWeight(BoneWeight bw, int armBone) {
        float weight = 0;
        weight += (armBone == bw.boneIndex0 ? bw.weight0 : 0);
        weight += (armBone == bw.boneIndex1 ? bw.weight1 : 0);
        weight += (armBone == bw.boneIndex2 ? bw.weight2 : 0);
        weight += (armBone == bw.boneIndex3 ? bw.weight3 : 0);
        return weight;
    }

    public static void DoConstructFakeArm (MenuCommand command, bool isRight)
    {
        Mesh sourceMesh;
        SkinnedMeshRenderer smr = null;
        if (command.context is SkinnedMeshRenderer) {
            smr = command.context as SkinnedMeshRenderer;
            sourceMesh = smr.sharedMesh;
        } else {
            EditorUtility.DisplayDialog ("MergeUVs", "Unknkown context type " + command.context.GetType ().FullName, "OK", "");
            throw new NotSupportedException ("Unknkown context type " + command.context.GetType ().FullName);
        }
        Transform animTransform = smr.transform.parent;
        Animator anim = null;
        string smrPath = smr.gameObject.name;
        while (animTransform != null) {
            anim = animTransform.GetComponent<Animator>();
            if (anim.avatar != null) {
                break;
            }
            smrPath = animTransform.name + "/" + smrPath;
            animTransform = animTransform.parent;
        }
        animTransform.gameObject.SetActive(true);
        Transform shoulderBone = anim.GetBoneTransform(isRight ? HumanBodyBones.RightShoulder : HumanBodyBones.LeftShoulder);
        Debug.Log(shoulderBone);

        GameObject clonedTopGO = GameObject.Instantiate(anim.gameObject);
        Animator clonedAnimator = clonedTopGO.GetComponent<Animator>();

        clonedAnimator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            AssetDatabase.GUIDToAssetPath (AssetDatabase.FindAssets("ConstructFakeArm_CurledArmAnimator")[0]));
        clonedAnimator.Rebind();
        clonedAnimator.Update(0);
        clonedAnimator.gameObject.SetActive(false);
        clonedAnimator.gameObject.SetActive(true);
        // Don't ask me why a newly created Animator will not update unless the
        // game object has been turned off and on again...
        // I wasted an hour on this before finding this trick.
        EditorApplication.delayCall += () => EditorApplication.delayCall += () => DoConstructFakeArmContinue(smr, sourceMesh, shoulderBone, anim, clonedTopGO, smrPath, isRight);
    }

    public static void DoConstructFakeArmContinue(SkinnedMeshRenderer smr, Mesh sourceMesh, Transform shoulderBone, Animator anim, GameObject clonedTopGO, string smrPath, bool isRight) {

        string pathToGenerated = "Assets" + "/Generated";
        if (!Directory.Exists (pathToGenerated)) {
            Directory.CreateDirectory (pathToGenerated);
        }

        float SHOULDER_HIDE_DUPLICATE_THRESH = 0.35f;
        float EXTRA_ARM_THRESH = 0.2f;

        Animator clonedAnimator = clonedTopGO.GetComponent<Animator>();
        clonedAnimator.Update(0);
        clonedTopGO.gameObject.SetActive(false);
        clonedTopGO.gameObject.SetActive(true);
        clonedAnimator.Update(0);

        SkinnedMeshRenderer clonedSMR = clonedTopGO.transform.Find(smrPath).GetComponent<SkinnedMeshRenderer>();
        Transform clonedArmBone = clonedAnimator.GetBoneTransform(isRight ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm);
        //Transform clonedShoulderBone = clonedAnimator.GetBoneTransform(isRight ? HumanBodyBones.RightShoulder : HumanBodyBones.LeftShoulder);
        GameObject smrParentGO = new GameObject("SMRParent");
        smrParentGO.transform.parent = clonedTopGO.transform;
        smrParentGO.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        smrParentGO.transform.localScale = new Vector3(100, 100, 100);
        smrParentGO.transform.parent = clonedArmBone;
        smrParentGO.transform.localPosition = new Vector3(0,0,0);
        GameObject smrParentBoneGO = new GameObject("SMRRootBone");
        smrParentBoneGO.transform.parent = smrParentGO.transform;
        smrParentBoneGO.transform.localRotation = Quaternion.identity;
        smrParentBoneGO.transform.localScale = new Vector3(1,1,1);
        smrParentBoneGO.transform.localPosition = new Vector3(0,0,0);
        // Note that we bake it into the arm bone instead of the shoulder to more closely match the rest pose.
        clonedSMR.transform.parent = smrParentGO.transform;
        clonedSMR.rootBone = smrParentBoneGO.transform;
        clonedSMR.transform.localRotation = Quaternion.identity;
        clonedSMR.transform.localScale = new Vector3(1,1,1);
        clonedSMR.transform.localPosition = new Vector3(0,0,0);
        Vector3 va = clonedTopGO.transform.localScale;
        Vector3 vb = clonedSMR.transform.lossyScale;
        clonedTopGO.transform.localScale = new Vector3(va.x/vb.x, va.y/vb.y, va.z/vb.z);

        Mesh poseBakedMesh = new Mesh();
        clonedSMR.BakeMesh(poseBakedMesh);
        GameObject.DestroyImmediate(clonedTopGO);

        HashSet<int> lowerArmBones = new HashSet<int>();
        HashSet<int> anyArmBones = new HashSet<int>();
        //int shoulderBoneIdx = -1;
        int armBoneIdx = -1;
        for (int i = 0; i < smr.bones.Count(); i++) {
            Transform p = (smr.bones[i]);
            if (p == shoulderBone) {
                //shoulderBoneIdx = i;
                anyArmBones.Add(i);
            }
            if (p.parent == shoulderBone) {
                armBoneIdx = i;
                anyArmBones.Add(i);
            }
            while (p.parent != null) {
                if (p.parent.parent == shoulderBone) {
                    lowerArmBones.Add(i);
                    anyArmBones.Add(i);
                    break;
                }
                p = p.parent;
            }
        }

        int size = sourceMesh.vertices.Length;
        List<Vector4> srcUV = new List<Vector4>();
        List<Vector4> srcUV2 = new List<Vector4>();
        List<Vector4> srcUV3 = new List<Vector4>();
        List<Vector4> srcUV4 = new List<Vector4>();
        sourceMesh.GetUVs (0, srcUV);
        sourceMesh.GetUVs (1, srcUV2);
        sourceMesh.GetUVs (2, srcUV3);
        sourceMesh.GetUVs (3, srcUV4);
        List<Vector3> srcVertices = new List<Vector3>();
        sourceMesh.GetVertices(srcVertices);
        List<Vector3> srcNormals = new List<Vector3>();
        sourceMesh.GetNormals(srcNormals);
        List<Vector4> srcTangents = new List<Vector4>();
        sourceMesh.GetTangents(srcTangents);
        Matrix4x4 [] srcBindposes = sourceMesh.bindposes;
        List<BoneWeight> srcBoneWeights = new List<BoneWeight>();
        sourceMesh.GetBoneWeights(srcBoneWeights);

        /////////////////////////////////////

        Dictionary<int, int> extraArmVertIdxMap = new Dictionary<int, int>();

        Vector3 [] poseBakedVertices = poseBakedMesh.vertices;
        Vector3 [] poseBakedNormals = poseBakedMesh.normals;
        Vector4 [] poseBakedTangents = poseBakedMesh.tangents;

        Mesh extraArmMesh = new Mesh();
        List<Vector3> extraArmVertices = new List<Vector3>();
        List<Vector3> extraArmNormals = new List<Vector3>();
        List<Vector4> extraArmTangents = new List<Vector4>();
        List<Vector4> extraArmUV = new List<Vector4>();
        List<Vector4> extraArmUV2 = new List<Vector4>();
        List<Vector4> extraArmUV3 = new List<Vector4>();
        List<Vector4> extraArmUV4 = new List<Vector4>();
        List<Color> extraArmColors = new List<Color>();
        
        float distanceToFingertip = 0;

        for (int i = 0; i < size; i++) {
            BoneWeight bw = srcBoneWeights[i];
            if (sumWeight(bw, anyArmBones) >= EXTRA_ARM_THRESH) {
                extraArmVertIdxMap[i] = extraArmVertices.Count;
                float curDist = poseBakedVertices[i].magnitude;
                distanceToFingertip = distanceToFingertip > curDist ? distanceToFingertip : curDist;
                extraArmVertices.Add(poseBakedVertices[i] / 1024.0f);
                float upperArmWeight = sumWeight(bw, armBoneIdx);
                //float shoulderWeight = sumWeight(bw, shoulderBoneIdx);
                float lowerArmWeight = sumWeight(bw, lowerArmBones);
                //extraArmColors.Add(new Color(lowerArmWeight, upperArmWeight, 1 - lowerArmWeight - upperArmWeight, 1));
                if (lowerArmWeight >= 0.8) {
                    extraArmColors.Add(Color.red);
                } else if (upperArmWeight >= 0.2) {
                    extraArmColors.Add(new Color(lowerArmWeight, 1 - lowerArmWeight, lowerArmWeight == 0 ? 1 - upperArmWeight : 0, 1));
                } else {
                   extraArmColors.Add(Color.white);
                }
                if (poseBakedNormals != null) {
                    extraArmNormals.Add(poseBakedNormals[i]);
                }
                if (poseBakedTangents != null) {
                    extraArmTangents.Add(poseBakedTangents[i]);
                }
                if (i < srcUV.Count) {
                    extraArmUV.Add(srcUV[i]);
                }
                if (i < srcUV2.Count) {
                    extraArmUV2.Add(srcUV2[i]);
                }
                if (i < srcUV3.Count) {
                    extraArmUV3.Add(srcUV3[i]);
                }
                if (i < srcUV4.Count) {
                    extraArmUV4.Add(srcUV4[i]);
                }
            }
        }

        extraArmMesh.SetVertices(extraArmVertices);
        if (extraArmNormals.Count > 0) {
            extraArmMesh.SetNormals(extraArmNormals);
        }
        if (extraArmTangents.Count > 0) {
            extraArmMesh.SetTangents(extraArmTangents);
        }
        if (extraArmUV.Count > 0) {
            extraArmMesh.SetUVs (0, extraArmUV);
        }
        if (extraArmUV2.Count > 0) {
            extraArmMesh.SetUVs (1, extraArmUV2);
        }
        if (extraArmUV3.Count > 0) {
            extraArmMesh.SetUVs (2, extraArmUV3);
        }
        if (extraArmUV4.Count > 0) {
            extraArmMesh.SetUVs (3, extraArmUV4);
        }
        extraArmMesh.SetColors(extraArmColors);
        extraArmMesh.subMeshCount = sourceMesh.subMeshCount;
        int extraArmMeshCount = 0;
        for (int i = 0; i < sourceMesh.subMeshCount; i++) {
            List<int> extraArmTris = new List<int>();
            var curIndices = sourceMesh.GetIndices (i);
            for (int idx = 0; idx < curIndices.Count(); idx += 3) {
                int v1, v2, v3;
                if (extraArmVertIdxMap.TryGetValue(curIndices[idx], out v1) &&
                        extraArmVertIdxMap.TryGetValue(curIndices[idx + 1], out v2) &&
                        extraArmVertIdxMap.TryGetValue(curIndices[idx + 2], out v3)) {
                    extraArmTris.Add(v1);
                    extraArmTris.Add(v2);
                    extraArmTris.Add(v3);
                }
            }
            if (extraArmTris.Any()) {
                extraArmMesh.SetIndices (extraArmTris.ToArray(), MeshTopology.Triangles, extraArmMeshCount);
                extraArmMeshCount++;
            }
        }
        extraArmMesh.subMeshCount = extraArmMeshCount;
        extraArmMesh.bounds = new Bounds(new Vector3(0,0,0), new Vector3(distanceToFingertip, distanceToFingertip, distanceToFingertip));
        extraArmMesh.name = sourceMesh.name + "_fakearm";


        GameObject extraGO = new GameObject(isRight ? "FakeRightArm" : "FakeLeftArm");
        extraGO.transform.parent = anim.transform;
        extraGO.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        extraGO.transform.localScale = new Vector3(100, 100, 100);
        Transform upperArmBone = anim.GetBoneTransform(isRight ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm);
        extraGO.transform.parent = upperArmBone.transform;
        extraGO.transform.localPosition = new Vector3(0,0,0);
        extraGO.transform.parent = shoulderBone.transform;
        // Parent to the shoulder bone, but keep the transform relative to the upperArmBone...
        MeshFilter extraArmMeshFilter = extraGO.AddComponent<MeshFilter>();
        extraArmMeshFilter.sharedMesh = extraArmMesh;
        extraArmMesh = extraArmMeshFilter.sharedMesh;
        MeshRenderer extraArmMeshRenderer = extraGO.AddComponent<MeshRenderer>();
        extraArmMeshRenderer.sharedMaterials = new Material[extraArmMeshCount];
        Undo.RegisterCreatedObjectUndo(extraArmMeshFilter.gameObject, "Create Fake Arm MeshRenderer");

        string fileName = pathToGenerated + "/constructFakeArm_extra_" + DateTime.UtcNow.ToString ("s").Replace (':', '_') + ".asset";
        AssetDatabase.CreateAsset (extraArmMesh, fileName);
        AssetDatabase.SaveAssets ();

        /////////////////////////////////////

        Mesh newMesh = new Mesh ();
        List<Color> newColors = new List<Color>();

        Dictionary<int, int> virginToOrigVerts = new Dictionary<int, int>();
        Dictionary<int, int> origToVirginVerts = new Dictionary<int, int>();
        int newVertCount = size;
        for (int i = 0; i < sourceMesh.subMeshCount; i++) {
            var curIndices = sourceMesh.GetIndices (i);
            for (int idx = 0; idx < curIndices.Count(); idx += 3) {
                int bwCount = 0;
                for (int k = 0; k < 3; k++) {
                    BoneWeight bwk = srcBoneWeights[curIndices[idx + k]];
                    if (sumWeight(bwk, lowerArmBones) + sumWeight(bwk, armBoneIdx) >= SHOULDER_HIDE_DUPLICATE_THRESH) {
                        bwCount++;
                    }
                }
                if (bwCount == 1 || bwCount == 2) {
                    for (int k = 0; k < 3; k++) {
                        int vert = curIndices[idx + k];
                        if (!origToVirginVerts.ContainsKey(vert)) {
                            origToVirginVerts.Add(vert, newVertCount);
                            virginToOrigVerts.Add(newVertCount, vert);
                            newVertCount++;
                        }
                    }
                }
            }
        }
        for (int i = 0; i < newVertCount; i++) {
            if (i >= size) {
                newColors.Add(Color.white);
                continue;
            }
            BoneWeight bw = srcBoneWeights[i];
            if (sumWeight(bw, lowerArmBones) + sumWeight(bw, armBoneIdx) >= SHOULDER_HIDE_DUPLICATE_THRESH) {
                newColors.Add(Color.red);
            } else {
                newColors.Add(Color.white);
            }
        }
        for (int i = size; i < newVertCount; i++) {
            srcVertices.Add(srcVertices[virginToOrigVerts[i]]);
        }
        newMesh.SetVertices(srcVertices);
        newMesh.SetColors(newColors);
        if (srcNormals != null && srcNormals.Count > 0) {
            for (int i = size; i < newVertCount; i++) {
                srcNormals.Add(srcNormals[virginToOrigVerts[i]]);
            }
            newMesh.SetNormals(srcNormals);
        }
        if (srcTangents != null && srcTangents.Count > 0) {
            for (int i = size; i < newVertCount; i++) {
                srcTangents.Add(srcTangents[virginToOrigVerts[i]]);
            }
            newMesh.SetTangents(srcTangents);
        }
        if (srcBoneWeights != null && srcBoneWeights.Count > 0) {
            for (int i = size; i < newVertCount; i++) {
                srcBoneWeights.Add(srcBoneWeights[virginToOrigVerts[i]]);
            }
            newMesh.boneWeights = srcBoneWeights.ToArray();
        }
        if (srcUV.Count > 0) {
            for (int i = size; i < newVertCount; i++) {
                srcUV.Add(srcUV[virginToOrigVerts[i]]);
            }
            newMesh.SetUVs (0, srcUV);
        }
        if (srcUV2.Count > 0) {
            for (int i = size; i < newVertCount; i++) {
                srcUV2.Add(srcUV2[virginToOrigVerts[i]]);
            }
            newMesh.SetUVs (1, srcUV2);
        }
        if (srcUV3.Count > 0) {
            for (int i = size; i < newVertCount; i++) {
                srcUV3.Add(srcUV3[virginToOrigVerts[i]]);
            }
            newMesh.SetUVs (2, srcUV3);
        }
        if (srcUV4.Count > 0) {
            for (int i = size; i < newVertCount; i++) {
                srcUV4.Add(srcUV4[virginToOrigVerts[i]]);
            }
            newMesh.SetUVs (3, srcUV4);
        }
        newMesh.subMeshCount = sourceMesh.subMeshCount;
        for (int i = 0; i < sourceMesh.subMeshCount; i++) {
            var curIndices = sourceMesh.GetIndices (i);
            for (int idx = 0; idx < curIndices.Count(); idx += 3) {
                int bwCount = 0;
                for (int k = 0; k < 3; k++) {
                    BoneWeight bwk = srcBoneWeights[curIndices[idx + k]];
                    if (sumWeight(bwk, lowerArmBones) + sumWeight(bwk, armBoneIdx) >= SHOULDER_HIDE_DUPLICATE_THRESH) {
                        bwCount++;
                    }
                }
                if (bwCount == 1 || bwCount == 2) {
                    for (int k = 0; k < 3; k++) {
                        curIndices[idx + k] = origToVirginVerts[curIndices[idx + k]];
                    }
                }
            }
            newMesh.SetIndices (curIndices, sourceMesh.GetTopology(i), i);
        }
        newMesh.bounds = sourceMesh.bounds;
        if (srcBindposes != null && srcBindposes.Length > 0) {
            newMesh.bindposes = sourceMesh.bindposes;
        }
        for (int i = 0; i < sourceMesh.blendShapeCount; i++) {
            var blendShapeName = sourceMesh.GetBlendShapeName (i);
            var blendShapeFrameCount = sourceMesh.GetBlendShapeFrameCount (i);
            for (int frameIndex = 0; frameIndex < blendShapeFrameCount; frameIndex++) {
                float weight = sourceMesh.GetBlendShapeFrameWeight(i, frameIndex);
                Vector3 [] deltaVertices = new Vector3 [size];
                Vector3 [] deltaNormals = new Vector3 [size];
                Vector3 [] deltaTangents = new Vector3 [size];
                sourceMesh.GetBlendShapeFrameVertices (i, frameIndex, deltaVertices, deltaNormals, deltaTangents);
                Vector3 [] newDeltaVertices = new Vector3 [newVertCount];
                Vector3 [] newDeltaNormals = new Vector3 [newVertCount];
                Vector3 [] newDeltaTangents = new Vector3 [newVertCount];
                for (int idx = 0; idx < newVertCount; idx++) {
                    int inIdx = idx < size ? idx : virginToOrigVerts[idx];
                    newDeltaVertices[idx] = deltaVertices[inIdx];
                    newDeltaNormals[idx] = deltaNormals[inIdx];
                    newDeltaTangents[idx] = deltaTangents[inIdx];
                }
                newMesh.AddBlendShapeFrame (blendShapeName, weight, newDeltaVertices, newDeltaNormals, newDeltaTangents);
            }
        }
        newMesh.name = sourceMesh.name + "_extraArmMain";
        Mesh meshAfterUpdate = newMesh;
        if (smr != null) {
            Undo.RecordObject (smr, "Updated SkinnedMeshRenderer and added MeshRenderer for fake arm");
            smr.sharedMesh = newMesh;
            meshAfterUpdate = smr.sharedMesh;
            // No need to change smr.bones: should use same bone indices and blendshapes.
        }
        fileName = pathToGenerated + "/constructFakeArm_main_" + DateTime.UtcNow.ToString ("s").Replace (':', '_') + ".asset";
        AssetDatabase.CreateAsset (meshAfterUpdate, fileName);
        AssetDatabase.SaveAssets ();
    }
}
#endif