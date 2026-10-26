using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceMiner
{
    // View-only right glove. ToolMount is a visual attachment seam for future tools;
    // this component does not implement a scanner, inventory or tool selection.
    [DefaultExecutionOrder(350)]
    public sealed class StationFirstPersonHands : MonoBehaviour
    {
        public const int ViewLayer = 31;
        public const float MaximumMotionOffset = .018f, MaximumMotionAngle = 4;
        private const int ViewMask = 1 << ViewLayer;
        public bool ReducedMotion;
        [Range(0, 1)] public float MotionStrength = 1;
        public bool IsVisible { get; private set; }
        public Camera ViewCamera { get; private set; }
        public Transform ModelRoot { get; private set; }
        public Transform ToolMount { get; private set; }
        public Vector3 MotionOffset => motionOffset;
        public Vector3 MotionAngles => motionAngles;
        public int VertexCount => mesh != null ? mesh.vertexCount : 0;
        private StationInteriorMode mode;
        private Camera source;
        private WaterScenario scenario;
        private Mesh mesh;
        private Texture2D cloth;
        private readonly List<Material> materials = new List<Material>();
        private Transform pose;
        private bool masked, sourceHadLayer, tracked;
        private Vector3 previousFeet, motionOffset, motionAngles;
        private float previousYaw, previousPitch, clock, stride, walkAmount;

        public void Initialize(StationInteriorMode owner, Camera camera)
        {
            if (source != null && source != camera) RestoreSourceMask();
            mode = owner; source = camera;
            scenario = FindFirstObjectByType<WaterScenario>();
            if (source == null) return;
            if (ViewCamera == null) Build();
            ExcludeFromSource(); TrackPose();
        }
        private void OnEnable() { if (source != null) ExcludeFromSource(); }
        private void OnDisable() { SetVisible(false); ResetMotion(); RestoreSourceMask(); }
        private void ExcludeFromSource()
        {
            if (source == null || masked) return;
            sourceHadLayer = (source.cullingMask & ViewMask) != 0;
            source.cullingMask &= ~ViewMask; masked = true;
        }
        private void RestoreSourceMask()
        {
            if (source != null && masked)
                source.cullingMask = sourceHadLayer ? source.cullingMask | ViewMask : source.cullingMask & ~ViewMask;
            masked = false;
        }
        public bool PresentationAllowed => mode != null && source != null && source.enabled
            && StationInteriorMode.IsInside && StationInteriorMode.Current == mode && !mode.ConsoleOpen
            && !SettingsMenu.BlocksInput && !StartMenu.IsOpen && !IntroSequence.BlocksGameplay
            && !MemoryCinematic.IsPlaying && (scenario == null || scenario.Scanner == null || scenario.Scanner.Dialogue == null);

        private void LateUpdate()
        {
            if (source == null || ViewCamera == null) return;
            ExcludeFromSource();
            bool visible = PresentationAllowed;
            if (visible != IsVisible) { ResetMotion(); TrackPose(); SetVisible(visible); }
            if (!visible) { TrackPose(); return; }
            // A short independent depth buffer protects the hand from nearby walls.
            // The main camera's near/far planes and projection are never changed here.
            ViewCamera.fieldOfView = source.fieldOfView; ViewCamera.aspect = source.aspect;
            ViewCamera.rect = source.rect; ViewCamera.targetDisplay = source.targetDisplay;
            ViewCamera.targetTexture = source.targetTexture; ViewCamera.depth = source.depth + 1;
            float seconds = Mathf.Min(Time.unscaledDeltaTime, .05f);
            Vector2 velocity = Vector2.zero, look = Vector2.zero;
            if (tracked && seconds > .00001f)
            {
                Vector3 delta = mode.FeetPosition - previousFeet;
                // Save/teleport changes are not walking input.
                if (delta.sqrMagnitude < 1)
                {
                    Vector3 local = source.transform.InverseTransformDirection(delta / seconds);
                    velocity = new Vector2(local.x, local.z);
                }
                look = new Vector2(Mathf.DeltaAngle(previousYaw, mode.ViewYaw), mode.ViewPitch - previousPitch) / seconds;
            }
            bool paused = SettingsMenu.PausesSimulation || scenario != null && scenario.SimulationRate <= 0;
            AdvanceMotion(seconds, velocity, look, mode.Vitals.IsSprinting, paused);
            TrackPose(); ApplyPose();
        }
        private void TrackPose()
        {
            if (mode == null) return;
            previousFeet = mode.FeetPosition; previousYaw = mode.ViewYaw; previousPitch = mode.ViewPitch; tracked = true;
        }
        public void AdvanceMotion(float seconds, Vector2 velocity, Vector2 lookVelocity, bool sprinting, bool paused)
        {
            if (paused || !Finite(seconds) || seconds <= 0) return;
            seconds = Mathf.Min(seconds, .05f);
            float strength = Finite(MotionStrength) ? Mathf.Clamp01(MotionStrength) : 0;
            if (ReducedMotion || strength <= 0) { ResetMotion(); ApplyPose(); return; }
            if (!Finite(velocity.x) || !Finite(velocity.y)) velocity = Vector2.zero;
            if (!Finite(lookVelocity.x) || !Finite(lookVelocity.y)) lookVelocity = Vector2.zero;
            float speed = Mathf.Clamp01(velocity.magnitude / 4);
            float smoothing = 1 - Mathf.Exp(-seconds * 11);
            walkAmount = Mathf.Lerp(walkAmount, speed, smoothing);
            clock += seconds; stride += seconds * (sprinting ? 10.5f : 8);
            Vector2 look = new Vector2(Mathf.Clamp(lookVelocity.x, -180, 180), Mathf.Clamp(lookVelocity.y, -180, 180));
            Vector3 target = new Vector3(Mathf.Sin(clock * 1.1f) * .00055f, Mathf.Sin(clock * 1.6f) * .0011f, 0)
                + new Vector3(Mathf.Sin(stride) * .0034f, Mathf.Cos(stride * 2) * .0038f, Mathf.Sin(stride) * .001f) * walkAmount
                + new Vector3(-look.x * .000038f, look.y * .000033f, 0);
            Vector3 angles = new Vector3(-look.y * .012f + Mathf.Sin(stride * 2) * walkAmount * .35f,
                -look.x * .013f, -look.x * .007f + Mathf.Sin(stride) * walkAmount * .65f + Mathf.Sin(clock) * .16f);
            motionOffset = Vector3.Lerp(motionOffset, Vector3.ClampMagnitude(target * strength, MaximumMotionOffset), smoothing);
            motionAngles = Vector3.Lerp(motionAngles, Vector3.ClampMagnitude(angles * strength, MaximumMotionAngle), smoothing);
            ApplyPose();
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private void ResetMotion() { motionOffset = motionAngles = Vector3.zero; walkAmount = clock = stride = 0; }
        private void ApplyPose()
        {
            if (pose == null || source == null) return;
            float aspectFactor = Mathf.Clamp(source.aspect / 1.6f, .6f, 1.3f);
            pose.localPosition = new Vector3(.24f * aspectFactor, -.19f, .46f) + motionOffset;
            pose.localRotation = Quaternion.Euler(new Vector3(16, -22, 58) + motionAngles);
        }
        private void SetVisible(bool value)
        {
            IsVisible = value;
            if (ViewCamera != null) ViewCamera.enabled = value;
            if (ModelRoot != null) ModelRoot.gameObject.SetActive(value);
        }
        private Material Surface(string name, Color color, float metal)
        {
            var material = new Material(Shader.Find("Standard")) { name = name, color = color };
            material.SetFloat("_Metallic", metal); material.SetFloat("_Glossiness", .08f);
            material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * .04f);
            materials.Add(material); return material;
        }
        private void Build()
        {
            var cameraObject = new GameObject("First person / glove overlay camera");
            cameraObject.transform.SetParent(source.transform, false);
            ViewCamera = cameraObject.AddComponent<Camera>(); ViewCamera.enabled = false;
            ViewCamera.clearFlags = CameraClearFlags.Depth; ViewCamera.cullingMask = ViewMask;
            // Render-only overlay: Unity's implicit OnMouseXXX pass must never
            // generate a second screen ray through this collider-free camera.
            ViewCamera.eventMask = 0;
            ViewCamera.nearClipPlane = .02f; ViewCamera.farClipPlane = 2;
            ViewCamera.allowHDR = false; ViewCamera.allowMSAA = source.allowMSAA;
            ViewCamera.useOcclusionCulling = false; ViewCamera.allowDynamicResolution = false;
            ModelRoot = new GameObject("First person / right pressure glove").transform;
            ModelRoot.SetParent(cameraObject.transform, false); ModelRoot.gameObject.layer = ViewLayer;
            pose = new GameObject("Glove / motion pose").transform; pose.SetParent(ModelRoot, false); pose.gameObject.layer = ViewLayer;
            var fabric = Surface("Work glove / grey textile fingers", new Color(.27f, .28f, .29f), .01f);
            var rubber = Surface("Work glove / dark flexible folds", new Color(.035f, .043f, .049f), .01f);
            var armour = Surface("Work glove / padded charcoal back", new Color(.105f, .125f, .14f), .015f);
            var sleeve = Surface("Suit sleeve / reinforced grey fabric", new Color(.24f, .26f, .28f), .015f);
            cloth = CreateCloth();
            foreach (var material in new[] { fabric, armour, sleeve }) material.mainTexture = cloth;
            var builder = new GloveMesh(); BuildGlove(builder); mesh = builder.Create();
            var model = new GameObject("Pressure glove / mesh", typeof(MeshFilter), typeof(MeshRenderer));
            model.layer = ViewLayer; model.transform.SetParent(pose, false); model.transform.localScale = Vector3.one * .9f;
            model.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = model.GetComponent<MeshRenderer>(); renderer.sharedMaterials = new[] { fabric, rubber, armour, sleeve };
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            ToolMount = new GameObject("Future tool / visual grip anchor").transform;
            ToolMount.SetParent(model.transform, false); ToolMount.localPosition = new Vector3(0, .055f, .043f);
            ToolMount.localRotation = Quaternion.Euler(0, 0, -90); ToolMount.gameObject.layer = ViewLayer;
            var lightObject = new GameObject("Glove / soft view light"); lightObject.transform.SetParent(ModelRoot, false);
            lightObject.transform.localRotation = Quaternion.Euler(25, -35, 0);
            var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional;
            light.color = new Color(.88f, .92f, .96f); light.intensity = 1; light.cullingMask = ViewMask; light.shadows = LightShadows.None;
            ApplyPose(); SetVisible(false);
        }
        private static void BuildGlove(GloveMesh mesh)
        {
            // The sleeve approaches from the lower right; soft folds and a wide
            // cuff replace the previous hard, pale wrist armour.
            mesh.Loft(new[] {new Vector3(-.11f,-.31f,.027f),new Vector3(-.075f,-.25f,.019f),new Vector3(-.042f,-.19f,.011f),
                new Vector3(-.023f,-.14f,.006f),new Vector3(-.009f,-.105f,.003f),new Vector3(0,-.075f,0)},
                new[] {new Vector2(.061f,.046f),new Vector2(.057f,.044f),new Vector2(.055f,.041f),
                    new Vector2(.050f,.038f),new Vector2(.047f,.035f),new Vector2(.042f,.033f)},3,.75f);
            for(int fold=0;fold<3;fold++)
            {
                float y=-.17f+fold*.035f,foldX=Mathf.Lerp(-.031f,-.007f,fold*.5f),width=.053f-fold*.004f;
                mesh.Loft(new[]{new Vector3(foldX,y-.006f,.006f),new Vector3(foldX,y,.006f),new Vector3(foldX,y+.006f,.006f)},
                    new[]{new Vector2(width,.039f),new Vector2(width+.002f,.041f),new Vector2(width-.002f,.037f)},3,.75f);
            }
            mesh.Loft(new[]{new Vector3(0,-.079f,0),new Vector3(0,-.065f,0),new Vector3(0,-.037f,0),new Vector3(0,-.027f,0)},
                new[]{new Vector2(.043f,.034f),new Vector2(.047f,.037f),new Vector2(.045f,.034f),new Vector2(.037f,.029f)},1,.65f);
            mesh.Loft(new[]{new Vector3(0,-.064f,-.033f),new Vector3(0,-.057f,-.038f),new Vector3(0,-.040f,-.034f)},
                new[]{new Vector2(.034f,.003f),new Vector2(.038f,.005f),new Vector2(.033f,.003f)},2,.5f);
            mesh.Loft(new[]{new Vector3(0,-.033f,0),Vector3.zero,new Vector3(0,.030f,0),new Vector3(0,.052f,0),new Vector3(0,.058f,0)},
                new[]{new Vector2(.034f,.025f),new Vector2(.043f,.027f),new Vector2(.052f,.026f),new Vector2(.052f,.024f),new Vector2(.047f,.022f)},2,.63f);
            mesh.Loft(new[]{new Vector3(0,-.005f,-.026f),new Vector3(0,.007f,-.033f),new Vector3(0,.035f,-.034f),new Vector3(0,.050f,-.026f)},
                new[]{new Vector2(.031f,.003f),new Vector2(.041f,.009f),new Vector2(.043f,.010f),new Vector2(.034f,.003f)},2,.6f);
            float[] x = {.041f,.014f,-.014f,-.040f}, radius={.038f,.041f,.038f,.032f}, curl={3.9f,4.1f,4.2f,4.25f};
            for (int finger = 0; finger < 4; finger++)
            {
                var points = new Vector3[12]; var radii = new Vector2[12];
                for (int ring = 0; ring < points.Length; ring++)
                {
                    float t=(float)ring/(points.Length-1),angle=t*curl[finger],baseY=finger==3?.050f:.053f;
                    points[ring]=new Vector3(x[finger]+(finger-1.5f)*t*.001f,baseY+Mathf.Sin(angle)*radius[finger],.043f-Mathf.Cos(angle)*radius[finger]);
                    float thickness=Mathf.Lerp(finger==3?.011f:.013f,.008f,t);
                    radii[ring]=new Vector2(thickness,thickness*.96f);
                }
                mesh.Loft(points, radii, 0, .8f);
                foreach (int joint in new[] { 3, 7 })
                {
                    Vector3 direction = (points[joint + 1] - points[joint]).normalized;
                    mesh.Loft(new[] { points[joint] - direction * .0009f, points[joint] + direction * .0009f },
                        new[] { radii[joint] * 1.025f, radii[joint] * 1.025f }, 1, .8f);
                }
                // Separate soft knuckle pads, without rigid finger plates.
                mesh.Loft(new[]{new Vector3(x[finger],.033f,-.025f),new Vector3(x[finger],.046f,-.030f),new Vector3(x[finger],.057f,-.023f)},
                    new[]{new Vector2(.010f,.003f),new Vector2(.012f,.006f),new Vector2(.009f,.003f)},2,.7f);
            }
            // Thumb lies across the upper grip, opposing the wrapped fingers.
            mesh.Loft(new[]{new Vector3(.040f,.006f,-.004f),new Vector3(.060f,.025f,-.010f),new Vector3(.065f,.048f,-.016f),
                new Vector3(.054f,.070f,-.017f),new Vector3(.033f,.085f,-.010f),new Vector3(.010f,.086f,.003f)},
                new[]{new Vector2(.021f,.020f),new Vector2(.021f,.019f),new Vector2(.019f,.017f),
                    new Vector2(.017f,.015f),new Vector2(.015f,.014f),new Vector2(.010f,.010f)},2,.8f);
            mesh.Loft(new[]{new Vector3(.055f,.068f,-.020f),new Vector3(.035f,.082f,-.016f),new Vector3(.017f,.085f,-.005f)},
                new[]{new Vector2(.009f,.003f),new Vector2(.011f,.005f),new Vector2(.006f,.003f)},0,.8f);
        }
        private static Texture2D CreateCloth()
        {
            const int size=128;var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float mottling=Mathf.PerlinNoise(x*.105f+3.7f,y*.105f+9.1f),grain=(x*13+y*37)%17/17f;
                float value=.76f+mottling*.18f+grain*.04f-((x+y/2)%4==0?.025f:0);
                pixels[y*size+x]=new Color(value,value,value,1);
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,true,true){name="Work glove / deterministic woven fabric",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear};
            texture.SetPixels(pixels);texture.Apply(true,true);return texture;
        }
        private void OnDestroy()
        {
            RestoreSourceMask();
            if (ViewCamera != null) Destroy(ViewCamera.gameObject);
            if (mesh != null) Destroy(mesh);
            if (cloth != null) Destroy(cloth);
            foreach (var material in materials) if (material != null) Destroy(material);
        }
        private sealed class GloveMesh
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector2> uv = new List<Vector2>();
            private readonly List<int>[] triangles = { new List<int>(), new List<int>(), new List<int>(), new List<int>() };
            public void Loft(Vector3[] path, Vector2[] radii, int material, float power)
            {
                const int sides = 12;
                int first = vertices.Count;
                Vector3 previousRight=Vector3.zero;
                for (int ring = 0; ring < path.Length; ring++)
                {
                    Vector3 tangent = (path[Mathf.Min(ring + 1, path.Length - 1)] - path[Mathf.Max(ring - 1, 0)]).normalized;
                    Vector3 right=ring==0?Vector3.Cross(tangent,Vector3.forward):previousRight-tangent*Vector3.Dot(previousRight,tangent);
                    if(right.sqrMagnitude<.001f)right=Vector3.Cross(tangent,Mathf.Abs(tangent.y)<.9f?Vector3.up:Vector3.right);
                    right.Normalize();previousRight=right;
                    Vector3 forward = Vector3.Cross(right, tangent).normalized;
                    for (int side = 0; side < sides; side++)
                    {
                        float angle = side * Mathf.PI * 2 / sides, c = Mathf.Cos(angle), s = Mathf.Sin(angle);
                        c = Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), power); s = Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), power);
                        vertices.Add(path[ring] + right * (c * radii[ring].x) + forward * (s * radii[ring].y));
                        uv.Add(new Vector2((float)side / sides, (float)ring / (path.Length - 1)));
                        if (ring == 0) continue;
                        int a = first + (ring - 1) * sides + side, b = first + (ring - 1) * sides + (side + 1) % sides;
                        int d = first + ring * sides + side, e = first + ring * sides + (side + 1) % sides;
                        triangles[material].AddRange(new[] { a, d, b, b, d, e });
                    }
                }
                int start = vertices.Count; vertices.Add(path[0]); uv.Add(Vector2.zero);
                int end = vertices.Count; vertices.Add(path[path.Length - 1]); uv.Add(Vector2.one);
                for (int side = 0; side < sides; side++)
                {
                    int next = (side + 1) % sides;
                    triangles[material].AddRange(new[] { start, first + side, first + next,
                        end, first + (path.Length - 1) * sides + next, first + (path.Length - 1) * sides + side });
                }
            }
            public Mesh Create()
            {
                var result = new Mesh { name = "First person / rounded pressure glove" };
                result.SetVertices(vertices); result.SetUVs(0, uv); result.subMeshCount = triangles.Length;
                for (int i = 0; i < triangles.Length; i++) result.SetTriangles(triangles[i], i);
                result.RecalculateNormals(); result.RecalculateBounds(); return result;
            }
        }
    }
}
