using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceMiner.Editor
{
    /// <summary>Imports a native right-hand counterpart of the accepted R03, preserving its source.</summary>
    public static class OperatorArmR03Importer
    {
        private const string Folder = "Assets/SpaceMiner/Resources/OperatorArmR03";
        private const string Source = "Design/Models/OperatorArm/Game-R03";
        [Serializable] private sealed class Joint
        {
            public string name;
            public int parent;
            public float[] head, tail;
        }
        [Serializable] private sealed class Marker
        {
            public string name;
            public int parent;
            public float[] position;
        }
        [Serializable] private sealed class Model
        {
            public string variant;
            public int triangles, unityVertices, skinBones;
            public Joint[] bones;
            public Marker[] markers;
        }

        [MenuItem("SpaceMiner/Models/Prepare accepted Operator Arm R03")]
        public static void Prepare()
        {
            var model = JsonUtility.FromJson<Model>(File.ReadAllText(Source + "/report.json"));
            if (model.bones == null || model.bones.Length != 17 || model.triangles > 55000)
                throw new InvalidDataException("Operator Arm R03 rig or triangle budget is invalid.");
            AssetDatabase.Refresh();
            Texture("BaseColor.jpg", false, true);
            Texture("Normal.png", true, false);
            Texture("MetalSmoothness.png", false, false);

            var root = new GameObject("OperatorArm_R03_Right");
            try
            {
                var skeleton = new GameObject("Skeleton").transform;
                skeleton.SetParent(root.transform, false);
                var bones = new Transform[model.bones.Length];
                for (int i = 0; i < bones.Length; i++)
                {
                    var joint = model.bones[i];
                    var bone = new GameObject(joint.name).transform;
                    bone.SetParent(joint.parent < 0 ? skeleton : bones[joint.parent], false);
                    bone.position = V(joint.head);
                    var direction = V(joint.tail) - bone.position;
                    bone.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                    bones[i] = bone;
                }
                foreach (var marker in model.markers)
                {
                    var tip = new GameObject(marker.name).transform;
                    tip.SetParent(bones[marker.parent], false);
                    tip.position = V(marker.position);
                    tip.rotation = bones[marker.parent].rotation;
                }

                var mesh = ReadMesh(model);
                var bind = new Matrix4x4[bones.Length];
                for (int i = 0; i < bind.Length; i++)
                    bind[i] = bones[i].worldToLocalMatrix * root.transform.localToWorldMatrix;
                mesh.bindposes = bind;
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                mesh = SaveMesh(mesh, Folder + "/ArmMesh.asset");

                var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/R03WorkSuit.mat");
                if (material == null)
                {
                    material = new Material(Shader.Find("Standard")) { name = "R03 navy textile and titanium" };
                    AssetDatabase.CreateAsset(material, Folder + "/R03WorkSuit.mat");
                }
                material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/BaseColor.jpg");
                material.color = Color.white;
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Normal.png"));
                material.SetFloat("_BumpScale", 1f);
                material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/MetalSmoothness.png"));
                material.SetFloat("_GlossMapScale", 1f);
                material.EnableKeyword("_NORMALMAP");
                material.EnableKeyword("_METALLICGLOSSMAP");
                EditorUtility.SetDirty(material);

                var skin = new GameObject("Accepted_R03_Skinned_Surface").AddComponent<SkinnedMeshRenderer>();
                skin.transform.SetParent(root.transform, false);
                skin.sharedMesh = mesh;
                skin.sharedMaterial = material;
                skin.bones = bones;
                skin.rootBone = bones[0];
                skin.quality = SkinQuality.Bone4;
                skin.shadowCastingMode = ShadowCastingMode.Off;
                skin.receiveShadows = false;
                // Runtime visibility is explicit; a first-person arm may cross its
                // rest bounding box during the two-bone console reach.
                skin.localBounds = new Bounds(new Vector3(0, .1f, .35f), Vector3.one * 1.8f);
                skin.updateWhenOffscreen = true;
                PrepareSleeve(root.transform, skeleton, bones[0], material);
                PrefabUtility.SaveAsPrefabAsset(root, Folder + "/Arm.prefab");
                AssetDatabase.SaveAssets();
                Debug.Log($"OPERATOR_ARM_R03_READY {mesh.vertexCount} vertices, {model.triangles} triangles, {bones.Length} bones");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void PrepareSleeve(Transform root, Transform skeleton, Transform upperArm, Material material)
        {
            var model = JsonUtility.FromJson<Model>(File.ReadAllText(Source + "/ShoulderSleeve-report.json"));
            if (model.skinBones != 2 || model.bones[1].name != "SleeveShoulder")
                throw new InvalidDataException("R03 shoulder continuation contract invalid.");
            var shoulder = new GameObject("SleeveShoulder").transform;
            shoulder.SetParent(skeleton, false);
            shoulder.localPosition = V(model.bones[1].head);
            shoulder.localRotation = Quaternion.identity;
            var bones = new[] { upperArm, shoulder };
            foreach (var marker in model.markers)
            {
                var point = new GameObject(marker.name).transform;
                point.SetParent(bones[marker.parent], false);
                point.position = V(marker.position);
            }
            var mesh = ReadMesh(model, "ShoulderSleeve.bin");
            mesh.bindposes = new[]
            {
                upperArm.worldToLocalMatrix * root.localToWorldMatrix,
                shoulder.worldToLocalMatrix * root.localToWorldMatrix
            };
            mesh.RecalculateBounds(); mesh.RecalculateTangents();
            mesh = SaveMesh(mesh, Folder + "/ShoulderSleeveMesh.asset");
            var sleeve = new GameObject("SleeveContinuation").AddComponent<SkinnedMeshRenderer>();
            sleeve.transform.SetParent(root, false);
            sleeve.sharedMesh = mesh; sleeve.sharedMaterial = SleeveMaterial(material); sleeve.bones = bones;
            sleeve.rootBone = shoulder; sleeve.quality = SkinQuality.Bone2;
            sleeve.shadowCastingMode = ShadowCastingMode.Off; sleeve.receiveShadows = false;
            sleeve.localBounds = new Bounds(new Vector3(0, .1f, 0), Vector3.one * 1.8f);
            sleeve.updateWhenOffscreen = true;
        }

        private static Material SleeveMaterial(Material arm)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/R03SleeveCloth.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Standard")) { name = "R03 matte navy sleeve cloth" };
                AssetDatabase.CreateAsset(material, Folder + "/R03SleeveCloth.mat");
            }
            material.mainTexture = arm.mainTexture;
            material.color = new Color(.78f, .82f, .90f, 1);
            // The authored atlas patch is textile. Reusing its full-arm normal
            // and metal maps stretched sharp shiny stripes along the connector.
            material.SetTexture("_BumpMap", null);
            material.SetTexture("_MetallicGlossMap", null);
            material.SetFloat("_Metallic", 0);
            material.SetFloat("_Glossiness", .18f);
            material.SetFloat("_SpecularHighlights", 0);
            material.SetFloat("_GlossyReflections", 0);
            material.DisableKeyword("_NORMALMAP");
            material.DisableKeyword("_METALLICGLOSSMAP");
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh SaveMesh(Mesh source, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(source, path); return source; }
            // CopySerialized can preserve an old larger vertex buffer when the
            // sleeve loses its former torso geometry. Rebuild every channel on
            // the same asset/GUID, so no rejected vertices or weights survive.
            existing.Clear(false);
            existing.name = source.name;
            existing.indexFormat = source.indexFormat;
            existing.vertices = source.vertices;
            existing.normals = source.normals;
            existing.uv = source.uv;
            existing.tangents = source.tangents;
            existing.boneWeights = source.boneWeights;
            existing.bindposes = source.bindposes;
            existing.triangles = source.triangles;
            existing.RecalculateBounds();
            if (existing.vertexCount != source.vertexCount || existing.triangles.Length != source.triangles.Length)
                throw new InvalidDataException("R03 mesh replacement retained stale vertex or index data: " + path);
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(source);
            return existing;
        }

        private static Mesh ReadMesh(Model model, string payload = "ArmMesh.bin")
        {
            using var reader = new BinaryReader(File.OpenRead(Source + "/" + payload));
            if (new string(reader.ReadChars(4)) != "AR03") throw new InvalidDataException("Wrong R03 mesh payload.");
            int vertexCount = reader.ReadInt32(), indexCount = reader.ReadInt32(), jointCount = reader.ReadInt32();
            if (vertexCount != model.unityVertices || indexCount != model.triangles * 3 || jointCount != model.skinBones)
                throw new InvalidDataException("R03 mesh/report mismatch.");
            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var uv = new Vector2[vertexCount];
            var weights = new BoneWeight[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                vertices[i] = ReadV(reader); normals[i] = ReadV(reader);
                uv[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                float w0 = reader.ReadSingle(), w1 = reader.ReadSingle(), w2 = reader.ReadSingle(), w3 = reader.ReadSingle();
                var weight = new BoneWeight
                {
                    weight0 = w0, weight1 = w1, weight2 = w2, weight3 = w3,
                    boneIndex0 = reader.ReadInt32(), boneIndex1 = reader.ReadInt32(),
                    boneIndex2 = reader.ReadInt32(), boneIndex3 = reader.ReadInt32()
                };
                if (Mathf.Abs(w0 + w1 + w2 + w3 - 1) > .0001f)
                    throw new InvalidDataException("R03 skin weight is not normalized.");
                weights[i] = weight;
            }
            var indices = new int[indexCount];
            // Reflection changes handedness: reverse each triangle so exterior
            // normals, backface culling and the recalculated tangent frame agree.
            for (int i = 0; i < indexCount; i += 3)
            { indices[i] = reader.ReadInt32(); indices[i + 2] = reader.ReadInt32(); indices[i + 1] = reader.ReadInt32(); }
            if (reader.BaseStream.Position != reader.BaseStream.Length) throw new InvalidDataException("Trailing R03 mesh data.");
            return new Mesh
            {
                name = "Accepted Operator Arm R03 / mirrored right gameplay skin",
                indexFormat = vertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
                vertices = vertices, normals = normals, uv = uv, boneWeights = weights, triangles = indices
            };
        }

        private static void Texture(string file, bool normal, bool srgb)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + "/" + file);
            if (importer == null) throw new FileNotFoundException("R03 gameplay texture missing", file);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = srgb;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.SaveAndReimport();
        }
        // Mirror mesh positions/normals, rig heads/tails and markers together.
        // Object and bone scales remain positive; the accepted left native
        // model and binary derivative are never overwritten by this import.
        private static Vector3 V(float[] p) => new Vector3(-p[0], p[1], p[2]);
        private static Vector3 ReadV(BinaryReader reader) => new Vector3(-reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }
}
