using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceMiner.Editor
{
    public static class MiraDialogueModelsBuild
    {
        public const string HighFolder = "Assets/SpaceMiner/AvatarLab/TripoH31";
        public const string ModelsFolder = "Assets/SpaceMiner/Resources/MiraModels";

        public static void Prepare()
        {
            Directory.CreateDirectory(ModelsFolder); AssetDatabase.Refresh();
            const string low = "Assets/SpaceMiner/AvatarLab/TripoH25";
            var lowMesh = AssetDatabase.LoadAssetAtPath<Mesh>(low + "/MiraTripoMesh.asset");
            var lowMaterial = AssetDatabase.LoadAssetAtPath<Material>(low + "/MiraTripoPBR.mat");
            if (lowMesh == null || lowMaterial == null || lowMesh.blendShapeCount != 13)
                throw new InvalidOperationException("Prepared H2.5 dialogue mesh/material/13 shapes missing");
            SavePrefab("Low", lowMesh, lowMaterial, .245f);
            var highMesh = ImportHigh();
            AddHighShapes(highMesh);
            AssetDatabase.CreateAsset(highMesh, HighFolder + "/MiraHighMesh.asset");
            var highMaterial = HighMaterial();
            AssetDatabase.CreateAsset(highMaterial, HighFolder + "/MiraHighPBR.mat");
            AssetDatabase.SaveAssets();
            SavePrefab("High", highMesh, highMaterial, .19f);
            Validate(highMesh);
            Debug.Log("MIRA MODELS PREPARED: independent H2.5 / H3.1 geometry and calibrated facial rigs");
        }

        private static void SavePrefab(string name, Mesh mesh, Material material, float pivot)
        {
            var root = new GameObject("Mira " + name + " dialogue model"); root.layer = 27;
            var driver = root.AddComponent<MiraAvatar>();
            var avatar = root.AddComponent<MiraAvatar3D>(); avatar.Driver = driver;
            avatar.enabled = false; avatar.EnhancedFace = true; avatar.JawWeight = 100;
            var bust = new GameObject("Mira bust").transform; bust.SetParent(root.transform, false); avatar.Bust = bust;
            var head = new GameObject("Mira head pivot").transform; head.SetParent(bust, false); head.localPosition = new Vector3(0, pivot, 0);
            var face = new GameObject("Mira " + name + " face"); face.layer = 27; face.transform.SetParent(bust, false);
            var skin = face.AddComponent<SkinnedMeshRenderer>(); skin.sharedMesh = mesh; skin.sharedMaterial = material;
            skin.bones = new[] { bust, head }; skin.rootBone = bust;
            skin.localBounds = new Bounds(mesh.bounds.center, mesh.bounds.size + Vector3.one * .04f);
            skin.updateWhenOffscreen = true; skin.enabled = false; avatar.Face = skin;
            var performance = root.AddComponent<MiraFacePerformance>(); performance.Avatar = avatar; performance.Head = head;
            if (name == "High")
            {
                Transform Bone(string label, Vector3 position)
                {
                    var bone = new GameObject(label).transform; bone.SetParent(bust, false); bone.localPosition = position;
                    return bone;
                }
                var torso = Bone("Mira upper body", new Vector3(0, .035f, 0));
                var left = Bone("Mira left shoulder", new Vector3(.13f, .12f, 0));
                var right = Bone("Mira right shoulder", new Vector3(-.13f, .12f, 0));
                head.SetParent(torso, true); left.SetParent(torso, true); right.SetParent(torso, true);
                performance.Torso = torso; performance.LeftShoulder = left; performance.RightShoulder = right;
                skin.bones = new[] { bust, head, torso, left, right };
            }
            PrefabUtility.SaveAsPrefabAsset(root, ModelsFolder + "/" + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static float Ease(float x) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(x));
        private static float Bell(float x) => Ease(1 - x * x);
        private static float Front(Vector3 v) => Ease((v.z - .055f) / .055f);
        private static void AddHighShapes(Mesh mesh)
        {
            // H3.1 landmarks measured in its own normalized front render, not H2.5 indices.
            // Mouth=(-.004,.278), eyes=(.035,.394)/(-.069,.378), roll slope=.15.
            var vertices = mesh.vertices;
            var weights = new BoneWeight[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                float head = Ease((v.y - .16f) / .09f);
                float upper = Ease((v.y - .015f) / .065f) * (1 - head);
                float shoulder = upper * Ease((Mathf.Abs(v.x) - .065f) / .09f) * (1 - Ease((v.y - .13f) / .06f)) * .8f;
                weights[i] = new BoneWeight { boneIndex0 = 0, weight0 = 1 - head - upper, boneIndex1 = 1, weight1 = head,
                    boneIndex2 = 2, weight2 = upper - shoulder, boneIndex3 = v.x >= 0 ? 3 : 4, weight3 = shoulder };
            }
            mesh.boneWeights = weights;
            mesh.bindposes = new[] { Matrix4x4.identity, Matrix4x4.Translate(new Vector3(0, -.19f, 0)),
                Matrix4x4.Translate(new Vector3(0, -.035f, 0)), Matrix4x4.Translate(new Vector3(-.13f, -.12f, 0)),
                Matrix4x4.Translate(new Vector3(.13f, -.12f, 0)) };
            var probe = new Mesh { indexFormat = mesh.indexFormat, vertices = vertices, triangles = mesh.triangles };
            probe.RecalculateNormals(); var baseNormals = probe.normals;
            void Shape(string name, Func<Vector3, Vector3> deform)
            {
                var delta = new Vector3[vertices.Length]; int affected = 0; float maximum = 0;
                for (int i = 0; i < delta.Length; i++)
                {
                    delta[i] = deform(vertices[i]); maximum = Mathf.Max(maximum, delta[i].magnitude);
                    if (delta[i].sqrMagnitude > 1e-10f) affected++;
                }
                if (affected < 10 || maximum > .021f) throw new InvalidOperationException("Invalid H3.1 shape " + name);
                var deformed = new Vector3[vertices.Length];
                for (int i = 0; i < deformed.Length; i++) deformed[i] = vertices[i] + delta[i];
                probe.vertices = deformed; probe.RecalculateNormals(); var normalDelta = probe.normals;
                for (int i = 0; i < normalDelta.Length; i++) normalDelta[i] -= baseNormals[i];
                mesh.AddBlendShapeFrame(name, 100, delta, normalDelta, null);
                Debug.Log("MIRA HIGH SHAPE " + name + ": vertices=" + affected + " max=" + maximum);
            }
            float Seam(Vector3 v) => .278f + .15f * (v.x + .004f);
            float Mouth(Vector3 v) => Bell((v.x + .004f) / .067f) * Bell((v.y - Seam(v)) / .031f) * Front(v);
            Shape("JawOpen", v => new Vector3(0, -.006f, .0008f) * Bell((v.x + .004f) / .078f)
                * Ease((Seam(v) - .001f - v.y) / .014f) * Ease((v.y - .195f) / .035f) * Front(v));
            Shape("LipClose", v => new Vector3(0, -Mathf.Clamp(v.y - Seam(v), -.009f, .009f) * .85f, 0)
                * Mouth(v) * Bell((v.y - Seam(v)) / .023f));
            Shape("Round", v => new Vector3(-(v.x + .004f) * .17f, 0, .0022f) * Mouth(v));
            Shape("Wide", v => new Vector3((v.x + .004f) * .08f, 0, -.0005f) * Mouth(v));
            Shape("Smile", v => new Vector3(Mathf.Sign(v.x + .004f) * .0012f, .0026f, 0)
                * Bell((Mathf.Abs(v.x + .004f) - .034f) / .026f) * Bell((v.y - Seam(v)) / .035f) * Front(v));
            Shape("BrowConcern", v => Vector3.up * .0026f * Front(v) *
                (Bell((v.x - .034f) / .039f) * Bell((v.y - (.425f + .15f * (v.x - .034f))) / .022f)
                + Bell((v.x + .069f) / .038f) * Bell((v.y - (.409f + .15f * (v.x + .069f))) / .022f)));
            Shape("BrowFocus", v => Vector3.down * .0016f * Front(v) * Bell((v.x + .017f) / .105f)
                * Bell((v.y - (.418f + .15f * (v.x + .017f))) / .024f));
            var eyes = new[] { new Vector2(.035f, .394f), new Vector2(-.069f, .378f) };
            for (int eye = 0; eye < eyes.Length; eye++)
            {
                var center = eyes[eye];
                string side=eye==0?"Left":"Right";
                float cornerX=eye==0?.030f:-.038f;
                Shape("Smile"+side,v=>new Vector3(eye==0?.002f:-.002f,.0054f,.0005f)
                    * Bell((v.x-cornerX)/.026f)*Bell((v.y-Seam(v))/.031f)*Front(v));
                Shape("CheekRaise"+side,v=>new Vector3(eye==0?.0007f:-.0007f,.0048f,.0013f)
                    *Bell((v.x-(center.x+(eye==0?.010f:-.010f)))/.035f)
                    *Bell((v.y-(center.y-.047f+.15f*(v.x-center.x)))/.032f)*Front(v));
                Shape("EyeSmile"+side,v=>new Vector3(0,.0024f,.0002f)
                    *Bell((v.x-center.x)/.036f)
                    *Bell((v.y-(center.y-.011f+.15f*(v.x-center.x)))/.010f)*Front(v));
                Shape("BrowLift"+side,v=>Vector3.up*.0048f
                    *Bell((v.x-center.x)/.042f)
                    *Bell((v.y-(center.y+.031f+.15f*(v.x-center.x)))/.021f)*Front(v));
                Shape(eye == 0 ? "BlinkLeft" : "BlinkRight", v => {
                    float x = (v.x - center.x) / .036f;
                    float line = center.y + .15f * (v.x - center.x) - .001f + .002f * Mathf.Min(1, x * x);
                    float y = v.y - line;
                    float close = y >= 0 ? Ease((.026f - y) / .013f) : Ease((.019f + y) / .009f);
                    return new Vector3(0, -y * .97f, 0) * close * Ease((1 - Mathf.Abs(x)) / .20f) * Ease((v.z - .035f) / .035f);
                });
            }
            float EyeMask(Vector3 v)
            {
                float sum = 0;
                foreach (var eye in eyes) sum += Bell((v.x - eye.x) / .026f) * Bell((v.y - (eye.y + .15f * (v.x - eye.x))) / .011f);
                return Mathf.Clamp01(sum) * Front(v);
            }
            Shape("LookLeft", v => Vector3.right * (.0017f * EyeMask(v)));
            Shape("LookRight", v => Vector3.left * (.0017f * EyeMask(v)));
            Shape("LookUp", v => Vector3.up * (.001f * EyeMask(v)));
            Shape("LookDown", v => Vector3.down * (.001f * EyeMask(v)));
            UnityEngine.Object.DestroyImmediate(probe);
        }

        private static void Validate(Mesh mesh)
        {
            if (mesh.blendShapeCount != 21 || mesh.boneWeights.Length != mesh.vertexCount || mesh.bindposes.Length != 5)
                throw new InvalidOperationException("H3.1 rig is incomplete");
            var delta = new Vector3[mesh.vertexCount]; var vertices = mesh.vertices;
            for (int shape = 0; shape < mesh.blendShapeCount; shape++)
            {
                mesh.GetBlendShapeFrameVertices(shape, 0, delta, null, null);
                for (int i = 0; i < vertices.Length; i++)
                    if (vertices[i].y < .15f && delta[i].sqrMagnitude > 1e-12f)
                        throw new InvalidOperationException("H3.1 face shape moves clothing/shoulders");
            }
        }

        private static Mesh ImportHigh()
        {
            AssetDatabase.Refresh();
            string path = HighFolder + "/Original.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new InvalidOperationException("Original Tripo H3.1 FBX missing");
            importer.isReadable = true; importer.importAnimation = false; importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            foreach (string file in new[] { "Color.jpg", "Normal.png", "Metallic.JPEG", "Roughness.JPEG" })
            {
                var texture = (TextureImporter)AssetImporter.GetAtPath(HighFolder + "/" + file);
                texture.maxTextureSize = 4096; texture.isReadable = true;
                texture.sRGBTexture = file == "Color.jpg";
                texture.textureType = file == "Normal.png" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                texture.textureCompression = TextureImporterCompression.CompressedHQ;
                texture.SaveAndReimport();
            }
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            var filter = root.GetComponentsInChildren<MeshFilter>().Single();
            var source = filter.sharedMesh;
            var vertices = source.vertices.Select(v => filter.transform.TransformPoint(v)).ToArray();
            var bounds = new Bounds(vertices[0], Vector3.zero);
            foreach (var v in vertices) bounds.Encapsulate(v);
            float scale = .62f / bounds.size.y;
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = (vertices[i] - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z)) * scale;
            var normalMatrix = filter.transform.localToWorldMatrix.inverse.transpose;
            var mesh = new Mesh { name = "Mira Tripo H3.1 dialogue mesh", indexFormat = IndexFormat.UInt32,
                vertices = vertices, uv = source.uv, triangles = source.triangles,
                normals = source.normals.Select(v => normalMatrix.MultiplyVector(v).normalized).ToArray() };
            mesh.RecalculateBounds(); mesh.RecalculateTangents();
            UnityEngine.Object.DestroyImmediate(root);
            return mesh;
        }

        private static Material HighMaterial()
        {
            var metal = AssetDatabase.LoadAssetAtPath<Texture2D>(HighFolder + "/Metallic.JPEG");
            var rough = AssetDatabase.LoadAssetAtPath<Texture2D>(HighFolder + "/Roughness.JPEG");
            var packed = new Texture2D(metal.width, metal.height, TextureFormat.RGBA32, false, true);
            var pixels = metal.GetPixels32(); var roughPixels = rough.GetPixels32();
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(pixels[i].r, pixels[i].r, pixels[i].r, (byte)(255 - roughPixels[i].r));
            packed.SetPixels32(pixels); packed.Apply();
            File.WriteAllBytes(HighFolder + "/MetalSmoothness.png", packed.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(packed); AssetDatabase.ImportAsset(HighFolder + "/MetalSmoothness.png");
            var importer = (TextureImporter)AssetImporter.GetAtPath(HighFolder + "/MetalSmoothness.png");
            importer.sRGBTexture = false; importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.CompressedHQ; importer.SaveAndReimport();
            var material = new Material(Shader.Find("Standard")) { name = "Mira H3.1 PBR", color = Color.white };
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(HighFolder + "/Color.jpg");
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(HighFolder + "/Normal.png"));
            material.SetFloat("_BumpScale", .22f); material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(HighFolder + "/MetalSmoothness.png"));
            material.EnableKeyword("_METALLICGLOSSMAP"); material.SetFloat("_GlossMapScale", .65f);
            return material;
        }

        public static void InspectHigh()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var mesh = ImportHigh(); var material = HighMaterial();
            var face = new GameObject("H3.1 inspection");
            face.AddComponent<MeshFilter>().sharedMesh = mesh; face.AddComponent<MeshRenderer>().sharedMaterial = material;
            var camera = new GameObject("Inspection camera").AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = .335f;
            camera.transform.position = new Vector3(0, .34f, 1.5f); camera.transform.LookAt(new Vector3(0, .34f, 0));
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f, .045f, .075f);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.36f,.36f,.38f);
            var key = new GameObject("Inspection key").AddComponent<Light>(); key.type = LightType.Directional;
            key.transform.rotation = Quaternion.Euler(25, 155, 0); key.intensity = 1.1f;
            var target = new RenderTexture(1200, 1200, 24); camera.targetTexture = target;
            camera.Render(); RenderTexture.active = target;
            var image = new Texture2D(1200, 1200, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0,0,1200,1200),0,0); image.Apply();
            Directory.CreateDirectory("Logs/MiraModels"); File.WriteAllBytes("Logs/MiraModels/high-neutral.png", image.EncodeToPNG());
            Debug.Log("MIRA H3.1 INSPECT: vertices=" + mesh.vertexCount + " bounds=" + mesh.bounds);
        }
    }
}
