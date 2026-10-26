#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceMiner
{
    // Shared measurements of the actual right-arm sleeve. The first ring follows
    // R03; the narrow proximal end continues beyond the view without a torso.
    internal static class OperatorArmSleeveAcceptance
    {
        [Serializable] internal struct Sample
        {
            public int joinVertices, proximalEndVertices, rigidNeckVertices;
            public float maximumJoinGap, proximalMarkerToMeshGap;
            public float maximumJoinBakeError, maximumJoinRendererMatrixBakeError, upperBindMatrixDifference;
            public Vector3 worstJoinRest, worstJoinBaked, worstJoinWorld, expectedJoinWorld;
            public Vector3 nearestCuffWorld, nearestCuffRest, armSkinPosition, sleeveSkinPosition;
            public Vector3 armUpperPosition, sleeveUpperPosition, armRendererMatrixPosition, sleeveRendererMatrixPosition;
            public Vector3 joinRestOrigin, joinRestNormal;
            public bool proximalEndOutsideView, sameUpperBone;
        }
        internal static SkinnedMeshRenderer OriginalArm(StationFirstPersonHands hands)
        {
            foreach (var skin in hands.ModelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (skin.sharedMesh != null && skin.sharedMesh.vertexCount == hands.VertexCount && skin.sharedMesh.vertexCount > 5000)
                    return skin;
            throw new InvalidOperationException("The original selected R03 arm skin is missing.");
        }
        internal static Sample Measure(StationFirstPersonHands hands, Camera view)
        {
            var arm = OriginalArm(hands); var sleeve = hands.SleeveContinuation;
            if (sleeve == null || sleeve.sharedMesh == null || hands.SleeveShoulder == null)
                throw new InvalidOperationException("The narrow authored R03 sleeve continuation is missing.");
            var lowerEdge = hands.SleeveShoulder.Find("SleeveProximalEnd");
            if (lowerEdge == null) throw new InvalidOperationException("The authored proximal sleeve-end marker is missing.");
            var source = new Mesh(); var cloth = new Mesh();
            try
            {
                arm.BakeMesh(source); sleeve.BakeMesh(cloth);
                Vector3[] armRest = arm.sharedMesh.vertices, armPose = source.vertices;
                var cuff = new List<Vector3>(); var cuffIndices = new List<int>();
                for (int i = 0; i < armRest.Length; i++)
                    if (armRest[i].z < .10f) { cuff.Add(arm.transform.TransformPoint(armPose[i])); cuffIndices.Add(i); }
                if (cuff.Count < 64) throw new InvalidOperationException("Actual R03 cuff surface is missing.");
                Vector3[] sleeveRest = sleeve.sharedMesh.vertices, posed = cloth.vertices;
                BoneWeight[] weights = sleeve.sharedMesh.boneWeights;
                int cuffBone = Array.FindIndex(sleeve.bones, bone => bone != null && bone.name == "UpperArm");
                int endBone = Array.IndexOf(sleeve.bones, hands.SleeveShoulder);
                if (cuffBone < 0 || endBone < 0) throw new InvalidOperationException("Continuation is disconnected from the real R03/sleeve-end bones.");
                var joinMarker = sleeve.bones[cuffBone].Find("SleeveCuffJoin");
                var joinAxis = sleeve.bones[cuffBone].Find("SleeveCuffJoinAxis");
                if (joinMarker == null || joinAxis == null || Vector3.Distance(joinMarker.position, joinAxis.position) < .001f)
                    throw new InvalidOperationException("The authored cuff-join plane markers are missing or degenerate.");
                // Classify the authored seam in bind space. Curved rigid neck
                // rings can pass within tenths of a millimetre of its plane;
                // world-space rounding and a broad slab would include them.
                var cuffToRest = sleeve.sharedMesh.bindposes[cuffBone].inverse;
                Vector3 joinRest = cuffToRest.MultiplyPoint3x4(joinMarker.localPosition);
                Vector3 joinRestNormal = (cuffToRest.MultiplyPoint3x4(joinAxis.localPosition) - joinRest).normalized;
                float endRadius = .012f;
                int armUpper = Array.FindIndex(arm.bones, bone => bone != null && bone.name == "UpperArm");
                var upperWorld = sleeve.bones[cuffBone].localToWorldMatrix * sleeve.sharedMesh.bindposes[cuffBone];
                var result = new Sample { proximalEndOutsideView = true, proximalMarkerToMeshGap = float.PositiveInfinity,
                    armSkinPosition = arm.transform.position, sleeveSkinPosition = sleeve.transform.position,
                    sameUpperBone = arm.bones[armUpper] == sleeve.bones[cuffBone],
                    armUpperPosition = arm.bones[armUpper].position, sleeveUpperPosition = sleeve.bones[cuffBone].position,
                    joinRestOrigin = joinRest, joinRestNormal = joinRestNormal,
                    armRendererMatrixPosition = arm.localToWorldMatrix.MultiplyPoint3x4(Vector3.zero),
                    sleeveRendererMatrixPosition = sleeve.localToWorldMatrix.MultiplyPoint3x4(Vector3.zero) };
                for (int entry = 0; entry < 16; entry++) result.upperBindMatrixDifference = Mathf.Max(result.upperBindMatrixDifference,
                    Mathf.Abs(arm.sharedMesh.bindposes[armUpper][entry] - sleeve.sharedMesh.bindposes[cuffBone][entry]));
                for (int i = 0; i < weights.Length; i++)
                {
                    BoneWeight w = weights[i]; Vector3 p = sleeve.transform.TransformPoint(posed[i]);
                    bool rigidToCuff = Influence(w, cuffBone) > .9999f;
                    bool onJoinPlane = Mathf.Abs(Vector3.Dot(sleeveRest[i] - joinRest, joinRestNormal)) < .00001f;
                    if (rigidToCuff && !onJoinPlane) result.rigidNeckVertices++;
                    // Extra rigid neck rings extend the sleeve past the cuff;
                    // only the authored first seam must neighbour original R03.
                    if (rigidToCuff && onJoinPlane)
                    {
                        result.joinVertices++; float gap = float.PositiveInfinity; int nearest = -1;
                        for (int c = 0; c < cuff.Count; c++)
                        { float next = Vector3.Distance(cuff[c], p); if (next < gap) { gap = next; nearest = c; } }
                        Vector3 expected = upperWorld.MultiplyPoint3x4(sleeveRest[i]);
                        result.maximumJoinBakeError = Mathf.Max(result.maximumJoinBakeError, Vector3.Distance(expected, p));
                        result.maximumJoinRendererMatrixBakeError = Mathf.Max(result.maximumJoinRendererMatrixBakeError,
                            Vector3.Distance(expected, sleeve.localToWorldMatrix.MultiplyPoint3x4(posed[i])));
                        if (gap > result.maximumJoinGap)
                        {
                            result.maximumJoinGap = gap; result.worstJoinRest = sleeveRest[i]; result.worstJoinBaked = posed[i];
                            result.worstJoinWorld = p; result.expectedJoinWorld = expected;
                            result.nearestCuffWorld = cuff[nearest]; result.nearestCuffRest = armRest[cuffIndices[nearest]];
                        }
                    }
                    if (Influence(w, endBone) > .9999f && Vector3.Distance(p, lowerEdge.position) < endRadius)
                    {
                        result.proximalEndVertices++;
                        result.proximalMarkerToMeshGap = Mathf.Min(result.proximalMarkerToMeshGap, Vector3.Distance(p, lowerEdge.position));
                        if (view.transform.InverseTransformPoint(p).z <= view.nearClipPlane) continue;
                        Vector3 viewport = view.WorldToViewportPoint(p);
                        if (viewport.x >= 0 && viewport.x <= 1 && viewport.y >= 0 && viewport.y <= 1)
                            result.proximalEndOutsideView = false;
                    }
                }
                return result;
            }
            finally { UnityEngine.Object.Destroy(source); UnityEngine.Object.Destroy(cloth); }
        }
        private static float Influence(BoneWeight w, int bone)
            => (w.boneIndex0 == bone ? w.weight0 : 0) + (w.boneIndex1 == bone ? w.weight1 : 0)
                + (w.boneIndex2 == bone ? w.weight2 : 0) + (w.boneIndex3 == bone ? w.weight3 : 0);
    }
}
#endif
