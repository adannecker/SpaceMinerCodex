using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceMiner
{
    public enum ConsoleTouchPhase { Idle, Reach, Contact, Retract }

    // The selected textured R03, mirrored as a right gameplay arm. Motion changes only presentation,
    // never the data or availability of the console that is being touched.
    [DefaultExecutionOrder(350)]
    public sealed class StationFirstPersonHands : MonoBehaviour
    {
        public const int ViewLayer = 31;
        public const float MaximumMotionOffset = .018f, MaximumMotionAngle = 4;
        private const int ViewMask = 1 << ViewLayer;
        private static readonly string[] TouchFingers = { "Index", "Middle", "Ring" };
        public bool ReducedMotion;
        [Range(0, 1)] public float MotionStrength = 1;
        public bool IsVisible { get; private set; }
        public Camera ViewCamera { get; private set; }
        public Transform ModelRoot { get; private set; }
        public Transform ToolMount { get; private set; }
        public Transform SleeveShoulder { get; private set; }
        public SkinnedMeshRenderer SleeveContinuation { get; private set; }
        public bool IsRightHand => true;
        // Only the arm is framed. A narrow clothing continuation leaves the
        // lower-right view behind the camera, without a visible torso or cuff.
        public Vector3 BodyShoulderPosition => source != null ? source.transform.TransformPoint(new Vector3(.43f,-.40f,-.10f)) : Vector3.zero;
        public Vector3 MotionOffset => motionOffset;
        public Vector3 MotionAngles => motionAngles;
        public int VertexCount => armRenderer != null && armRenderer.sharedMesh != null ? armRenderer.sharedMesh.vertexCount : 0;
        public bool HasAuthoredArm => armRenderer != null && upper != null && forearm != null && wrist != null && tips[0] != null && tips[1] != null && tips[2] != null;
        public bool TouchInProgress => TouchPhase != ConsoleTouchPhase.Idle;
        public ConsoleTouchPhase TouchPhase { get; private set; }
        public StationConsoleTouchSurface ActiveTouchSurface { get; private set; }
        public TouchConsoleVisual ActiveConsoleVisual => ActiveTouchSurface != null ? ActiveTouchSurface.ConsoleVisual : null;
        public Vector3 ContactPoint { get; private set; }
        public Vector3 ContactNormal { get; private set; }
        public Vector3[] TouchPoints { get; } = new Vector3[3];
        public int CompletedTouches { get; private set; }
        public int CancelledTouches { get; private set; }
        private StationInteriorMode mode;
        private Camera source;
        private WaterScenario scenario;
        private Transform pose, upper, forearm, wrist;
        private readonly Transform[] tips = new Transform[3], fingerBases = new Transform[3];
        private readonly List<PadProbe>[] pads = { new List<PadProbe>(), new List<PadProbe>(), new List<PadProbe>() };
        private readonly float[] contactAngles = { 5, 5, 5 };
        private readonly Dictionary<Transform, Quaternion> rest = new Dictionary<Transform, Quaternion>();
        private readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        private SkinnedMeshRenderer armRenderer;
        private float upperLength, forearmLength;
        private Quaternion restWristRotation;
        private Vector3 neutralMiddleFromWrist;
        private bool masked, sourceHadLayer, tracked;
        private Vector3 previousFeet, motionOffset, motionAngles, touchLocalPoint, touchLocalNormal;
        private Vector3 startWrist, startUpperAnchor, startFeet;
        private bool touchHadFocus;
        private Quaternion startWristRotation;
        private float previousYaw, previousPitch, clock, stride, walkAmount, touchTime, startYaw, startPitch;
        private Action openAfterTouch;
        private int touchStartedFrame = -1;

        public void Initialize(StationInteriorMode owner, Camera camera)
        {
            if (source != null && source != camera) RestoreSourceMask();
            mode = owner; source = camera; scenario = FindFirstObjectByType<WaterScenario>();
            if (source == null) return;
            if (ViewCamera == null) Build();
            ExcludeFromSource(); TrackPose();
        }
        private void OnEnable() { if (source != null) ExcludeFromSource(); }
        private void OnDisable() { CancelTouch(); SetVisible(false); ResetMotion(); RestoreSourceMask(); }
        private void ExcludeFromSource()
        {
            if (source == null || masked) return;
            sourceHadLayer = (source.cullingMask & ViewMask) != 0;
            source.cullingMask &= ~ViewMask; masked = true;
        }
        private void RestoreSourceMask()
        {
            if (source != null && masked) source.cullingMask = sourceHadLayer ? source.cullingMask | ViewMask : source.cullingMask & ~ViewMask;
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
            if(TouchInProgress && Application.isFocused)touchHadFocus=true;
            if (TouchInProgress && (!PresentationAllowed || touchHadFocus && !Application.isFocused || ActiveTouchSurface == null || !ActiveTouchSurface.isActiveAndEnabled)) CancelTouch();
            bool visible = PresentationAllowed && HasAuthoredArm;
            if (visible != IsVisible) { ResetMotion(); TrackPose(); SetVisible(visible); }
            if (!visible) { TrackPose(); return; }
            ViewCamera.fieldOfView = source.fieldOfView; ViewCamera.aspect = source.aspect;
            ViewCamera.rect = source.rect; ViewCamera.targetDisplay = source.targetDisplay;
            ViewCamera.targetTexture = source.targetTexture; ViewCamera.depth = source.depth + 1;
            // A contact uses the world's depth and projection: the actual glass
            // and walls can occlude the mesh. Idle keeps its short overlay buffer.
            ViewCamera.clearFlags = TouchInProgress ? CameraClearFlags.Nothing : CameraClearFlags.Depth;
            ViewCamera.nearClipPlane = TouchInProgress ? source.nearClipPlane : .02f;
            ViewCamera.farClipPlane = TouchInProgress ? source.farClipPlane : 2;
            float seconds = Mathf.Min(Time.unscaledDeltaTime, .05f);
            if (TouchInProgress) { AdvanceTouch(seconds); TrackPose(); return; }
            Vector2 velocity = Vector2.zero, look = Vector2.zero;
            if (tracked && seconds > .00001f)
            {
                Vector3 delta = mode.FeetPosition - previousFeet;
                if (delta.sqrMagnitude < 1)
                {
                    Vector3 local = source.transform.InverseTransformDirection(delta / seconds);
                    velocity = new Vector2(local.x, local.z);
                }
                look = new Vector2(Mathf.DeltaAngle(previousYaw, mode.ViewYaw), mode.ViewPitch - previousPitch) / seconds;
            }
            bool paused = SettingsMenu.PausesSimulation || scenario != null && scenario.SimulationRate <= 0;
            AdvanceMotion(seconds, velocity, look, mode.Vitals.IsSprinting, paused);
            TrackPose(); ApplyIdlePose();
        }
        private void TrackPose()
        {
            if (mode == null) return;
            previousFeet = mode.FeetPosition; previousYaw = mode.ViewYaw; previousPitch = mode.ViewPitch; tracked = true;
        }
        public void AdvanceMotion(float seconds, Vector2 velocity, Vector2 lookVelocity, bool sprinting, bool paused)
        {
            if (paused || TouchInProgress || !Finite(seconds) || seconds <= 0) return;
            seconds = Mathf.Min(seconds, .05f);
            float strength = Finite(MotionStrength) ? Mathf.Clamp01(MotionStrength) : 0;
            if (ReducedMotion || strength <= 0) { ResetMotion(); ApplyIdlePose(); return; }
            if (!Finite(velocity.x) || !Finite(velocity.y)) velocity = Vector2.zero;
            if (!Finite(lookVelocity.x) || !Finite(lookVelocity.y)) lookVelocity = Vector2.zero;
            float speed = Mathf.Clamp01(velocity.magnitude / 4), smoothing = 1 - Mathf.Exp(-seconds * 11);
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
            ApplyIdlePose();
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private void ResetMotion() { motionOffset = motionAngles = Vector3.zero; walkAmount = clock = stride = 0; }
        private Vector3 UpperAnchor => source.transform.position + Quaternion.Euler(0, mode.ViewYaw, 0) * new Vector3(.23f, -.50f, .09f);
        private void ResetRig() { foreach (var pair in rest) pair.Key.localRotation = pair.Value; }
        private void Curl(string name, float degrees)
        { if (bones.TryGetValue(name, out var bone)) bone.localRotation = rest[bone] * Quaternion.Euler(degrees, 0, 0); }
        private void FingerPose(float blend)
        {
            for(int i=0;i<TouchFingers.Length;i++)
            {string finger=TouchFingers[i];Curl(finger + "01", Mathf.Lerp(20, contactAngles[i], blend)); Curl(finger + "02", Mathf.Lerp(25, 5, blend)); Curl(finger + "03", Mathf.Lerp(12, 3, blend)); }
            Curl("Thumb01", 18); Curl("Thumb02", 15); Curl("Little01", 32); Curl("Little02", 35); Curl("Little03", 18);
        }
        private void ApplyIdlePose()
        {
            if (!HasAuthoredArm || source == null || mode == null || TouchInProgress) return;
            ResetRig(); FingerPose(0);
            float aspectFactor = Mathf.Clamp(source.aspect / 1.6f, .6f, 1.3f);
            Vector3 wristPosition = source.transform.TransformPoint(new Vector3(.23f * aspectFactor, -.29f, .42f) + motionOffset);
            Quaternion rotation = source.transform.rotation * Quaternion.Euler(new Vector3(-16, -7, 7) + motionAngles);
            SolveArm(UpperAnchor, wristPosition, rotation * restWristRotation);
        }
        private void SolveArm(Vector3 anchor, Vector3 wristPosition, Quaternion wristRotation)
        {
            // Analytic two-bone IK changes rotations only, never mesh or bone scale.
            pose.SetPositionAndRotation(anchor, Quaternion.Euler(0, mode.ViewYaw, 0));
            pose.position += anchor - upper.position;
            Vector3 delta = wristPosition - upper.position;
            float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(upperLength - forearmLength) + .001f, upperLength + forearmLength - .001f);
            Vector3 direction = delta.normalized;
            Vector3 pole = Quaternion.Euler(0, mode.ViewYaw, 0) * new Vector3(1, -.45f, -.1f);
            pole = Vector3.ProjectOnPlane(pole, direction).normalized;
            float along = (upperLength * upperLength - forearmLength * forearmLength + distance * distance) / (2 * distance);
            Vector3 elbow = upper.position + direction * along + pole * Mathf.Sqrt(Mathf.Max(0, upperLength * upperLength - along * along));
            upper.rotation = Quaternion.FromToRotation(forearm.position - upper.position, elbow - upper.position) * upper.rotation;
            Vector3 end = upper.position + direction * distance;
            forearm.rotation = Quaternion.FromToRotation(wrist.position - forearm.position, end - forearm.position) * forearm.rotation;
            wrist.rotation = wristRotation;
            // Continue only the sleeve, not a chest model, beyond the view. The
            // distal ring follows the exact R03 cuff throughout the reach.
            // Equal skin rotations preserve its round section; blending cuff
            // rotation with an unrelated torso yaw flattened it into a ribbon.
            if(SleeveShoulder!=null)SleeveShoulder.SetPositionAndRotation(BodyShoulderPosition,
                upper.rotation * Quaternion.Inverse(rest[upper]));
        }
        private bool ContactPose(StationConsoleTouchSurface surface, Vector3 point, Vector3 normal, out Vector3 wristPosition, out Quaternion wristRotation)
        {
            Quaternion hand = Quaternion.LookRotation(surface.Forward * .50f - normal * .8660254f, normal);
            // The ring finger is shorter; leave 14 mm of articulation room rather
            // than stretching it to the middle finger's neutral contact plane.
            wristPosition = point - hand * neutralMiddleFromWrist - normal * .014f;
            wristRotation = hand * restWristRotation;
            float distance = Vector3.Distance(TouchInProgress?startUpperAnchor:UpperAnchor, wristPosition);
            Vector3 toPoint = point - source.transform.position;
            return distance <= upperLength + forearmLength - .008f && distance >= Mathf.Abs(upperLength - forearmLength) + .008f
                && Vector3.Dot(source.transform.forward, toPoint.normalized) > .1f;
        }
        public bool CanReachConsole(StationConsoleTouchSurface surface)
        {
            return HasAuthoredArm && source != null && surface != null && surface.VisibleFrom(source) && surface.TryGetTouchPoint(source, out var point, out var normal)
                && ContactPose(surface, point, normal, out _, out _);
        }
        public bool BeginTouch(StationConsoleTouchSurface surface, Action open)
        {
            if (TouchInProgress || !PresentationAllowed || open == null || !CanReachConsole(surface)) return false;
            surface.TryGetTouchPoint(source, out var point, out var normal);
            ActiveTouchSurface = surface; touchLocalPoint = surface.transform.InverseTransformPoint(point);
            touchLocalNormal = surface.transform.InverseTransformDirection(normal); ContactPoint = point; ContactNormal = normal;
            // Solve once when E is pressed, using real weighted glove vertices.
            // Subsequent animation frames interpolate these angles without a
            // per-frame mesh bake or repeated iterative inverse kinematics.
            ContactPose(surface,point,normal,out var contactWrist,out var contactRotation);
            ResetRig();FingerPose(1);SolveArm(UpperAnchor,contactWrist,contactRotation);
            for(int i=0;i<tips.Length;i++)contactAngles[i]=SolveFingerContact(i);
            ApplyIdlePose(); startWrist = wrist.position; startWristRotation = wrist.rotation; startUpperAnchor = UpperAnchor;
            startYaw = mode.ViewYaw; startPitch = mode.ViewPitch;startFeet=mode.FeetPosition;touchHadFocus=Application.isFocused;
            openAfterTouch = open; touchTime = 0; touchStartedFrame = Time.frameCount; TouchPhase = ConsoleTouchPhase.Reach;
            return true;
        }
        public void CancelTouch()
        {
            if (TouchInProgress) CancelledTouches++;
            TouchPhase = ConsoleTouchPhase.Idle; ActiveTouchSurface = null; openAfterTouch = null; touchTime = 0;
        }
        public void AdvanceTouch(float seconds)
        {
            if (!TouchInProgress || !Finite(seconds) || seconds <= 0 || Time.frameCount == touchStartedFrame) return;
            if (!PresentationAllowed || ActiveTouchSurface == null) { CancelTouch(); return; }
            ContactPoint = ActiveTouchSurface.transform.TransformPoint(touchLocalPoint);
            ContactNormal = ActiveTouchSurface.transform.TransformDirection(touchLocalNormal).normalized;
            if (!ContactPose(ActiveTouchSurface, ContactPoint, ContactNormal, out var target, out var rotation)
                || Vector3.Distance(mode.FeetPosition, startFeet) > .035f || !ActiveTouchSurface.VisibleFrom(source)) { CancelTouch(); return; }
            float reach = ReducedMotion ? .22f : .55f, hold = .20f, retract = ReducedMotion ? .14f : .25f;
            touchTime += Mathf.Min(seconds, .05f);
            float amount;
            if (touchTime < reach) { TouchPhase = ConsoleTouchPhase.Reach; amount = Mathf.SmoothStep(0, 1, touchTime / reach); }
            else if (touchTime < reach + hold) { TouchPhase = ConsoleTouchPhase.Contact; amount = 1; }
            else { TouchPhase = ConsoleTouchPhase.Retract; amount = 1 - .25f * Mathf.SmoothStep(0, 1, (touchTime - reach - hold) / retract); }
            mode.FocusTouchPoint(ContactPoint, startYaw, startPitch, Mathf.SmoothStep(0, 1, Mathf.Min(1, touchTime / reach)));
            ResetRig(); FingerPose(amount);
            SolveArm(startUpperAnchor, Vector3.Lerp(startWrist, target, amount), Quaternion.Slerp(startWristRotation, rotation, amount));
            for (int i = 0; i < tips.Length; i++)
            { if(TouchPhase==ConsoleTouchPhase.Contact)PadDistance(i,out TouchPoints[i]);else TouchPoints[i]=tips[i].position; }
            if (touchTime >= reach + hold + retract)
            {
                Action complete = openAfterTouch;
                TouchPhase = ConsoleTouchPhase.Idle; ActiveTouchSurface = null; openAfterTouch = null;
                CompletedTouches++; complete?.Invoke();
                SetVisible(PresentationAllowed && HasAuthoredArm);
            }
        }
        private float SolveFingerContact(int index)
        {
            Transform bone = fingerBases[index]; Quaternion bind = rest[bone];
            float best = 0, bestError = float.MaxValue, previousAngle = -40, previousDistance = 0;
            for (int sample = 0; sample <= 20; sample++)
            {
                float angle = -40 + sample * 5; bone.localRotation = bind * Quaternion.Euler(angle, 0, 0);
                float distance = PadDistance(index,out _);
                if (Mathf.Abs(distance) < bestError) { best = angle; bestError = Mathf.Abs(distance); }
                if (sample > 0 && previousDistance * distance <= 0)
                {
                    float low = previousAngle, high = angle;
                    for (int step = 0; step < 16; step++)
                    {
                        float middle = (low + high) * .5f; bone.localRotation = bind * Quaternion.Euler(middle, 0, 0);
                        float signed = PadDistance(index,out _);
                        if (signed * previousDistance > 0) low = middle; else high = middle;
                    }
                    best = (low + high) * .5f; break;
                }
                previousAngle = angle; previousDistance = distance;
            }
            bone.localRotation = bind * Quaternion.Euler(best, 0, 0);
            return best;
        }
        private float PadDistance(int finger,out Vector3 point)
        {
            point=tips[finger].position;float minimum=float.PositiveInfinity;
            foreach(var probe in pads[finger])
            {Vector3 p=probe.Point();float distance=Vector3.Dot(p-ContactPoint,ContactNormal);if(distance<minimum){minimum=distance;point=p;}}
            return float.IsPositiveInfinity(minimum)?Vector3.Dot(point-ContactPoint,ContactNormal):minimum;
        }
        private sealed class PadProbe
        {
            public readonly Transform[] bones=new Transform[4];
            public readonly Vector3[] local=new Vector3[4];
            public readonly float[] weight=new float[4];
            public Vector3 Point()
            {Vector3 p=Vector3.zero;for(int i=0;i<4;i++)if(weight[i]>0)p+=bones[i].TransformPoint(local[i])*weight[i];return p;}
        }
        private void CachePadProbes()
        {
            var mesh=armRenderer.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var bind=mesh.bindposes;
            for(int vertex=0;vertex<vertices.Length;vertex++)
            {
                var w=weights[vertex];int[] indices={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};float[] values={w.weight0,w.weight1,w.weight2,w.weight3};
                int dominant=0;for(int j=1;j<4;j++)if(values[j]>values[dominant])dominant=j;
                for(int finger=0;finger<tips.Length;finger++)
                {
                    if(armRenderer.bones[indices[dominant]]!=tips[finger].parent
                        || Vector3.Distance(armRenderer.transform.TransformPoint(vertices[vertex]),tips[finger].position)>.04f)continue;
                    var probe=new PadProbe();
                    for(int j=0;j<4;j++){probe.bones[j]=armRenderer.bones[indices[j]];probe.local[j]=bind[indices[j]].MultiplyPoint3x4(vertices[vertex]);probe.weight[j]=values[j];}
                    pads[finger].Add(probe);
                }
            }
        }
        private void SetVisible(bool value)
        { IsVisible = value; if (ViewCamera != null) ViewCamera.enabled = value; if (ModelRoot != null) ModelRoot.gameObject.SetActive(value); }
        private void Build()
        {
            var cameraObject = new GameObject("First person / R03 right arm camera"); cameraObject.transform.SetParent(source.transform, false);
            ViewCamera = cameraObject.AddComponent<Camera>(); ViewCamera.enabled = false;
            ViewCamera.clearFlags = CameraClearFlags.Depth; ViewCamera.cullingMask = ViewMask;
            ViewCamera.eventMask = 0; ViewCamera.nearClipPlane = .02f; ViewCamera.farClipPlane = 2;
            ViewCamera.allowHDR = false; ViewCamera.allowMSAA = source.allowMSAA;
            ViewCamera.useOcclusionCulling = false; ViewCamera.allowDynamicResolution = false;
            ModelRoot = new GameObject("First person / selected R03 right operator arm").transform;
            ModelRoot.SetParent(cameraObject.transform, false); ModelRoot.gameObject.layer = ViewLayer;
            pose = new GameObject("R03 / arm motion pose").transform; pose.SetParent(ModelRoot, false);
            var prefab = Resources.Load<GameObject>("OperatorArmR03/Arm");
            if (prefab == null) { Debug.LogError("Selected R03 arm asset missing. Run OperatorArmR03Importer.Prepare before building."); SetVisible(false); return; }
            var arm = Instantiate(prefab, pose, false); arm.name = "OperatorArm R03 / rigged right arm";
            foreach (var t in arm.GetComponentsInChildren<Transform>()) { t.gameObject.layer = ViewLayer; bones[t.name] = t; rest[t] = t.localRotation; }
            bones.TryGetValue("UpperArm", out upper); bones.TryGetValue("Forearm", out forearm); bones.TryGetValue("Wrist", out wrist);
            bones.TryGetValue("SleeveShoulder",out var shoulder);SleeveShoulder=shoulder;
            foreach(var skin in arm.GetComponentsInChildren<SkinnedMeshRenderer>())
                if(skin.gameObject.name=="SleeveContinuation")SleeveContinuation=skin;
            for (int i = 0; i < TouchFingers.Length; i++) { bones.TryGetValue("Tip" + TouchFingers[i], out tips[i]); bones.TryGetValue(TouchFingers[i] + "01", out fingerBases[i]); }
            armRenderer = arm.GetComponentInChildren<SkinnedMeshRenderer>();
            if (!HasAuthoredArm) { Debug.LogError("Selected R03 arm rig is incomplete."); SetVisible(false); return; }
            upperLength = Vector3.Distance(upper.position, forearm.position); forearmLength = Vector3.Distance(forearm.position, wrist.position);
            restWristRotation = Quaternion.Inverse(pose.rotation) * wrist.rotation;
            CachePadProbes();
            ResetRig(); FingerPose(1); neutralMiddleFromWrist = pose.InverseTransformDirection(tips[1].position - wrist.position);
            foreach (var r in arm.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off; }
            ToolMount = new GameObject("Future tool / R03 right grip anchor").transform;
            ToolMount.SetParent(wrist, false); ToolMount.gameObject.layer = ViewLayer; ToolMount.localPosition = new Vector3(0, -.025f, .1f);
            var lightObject = new GameObject("R03 / soft view light"); lightObject.transform.SetParent(ModelRoot, false); lightObject.transform.localRotation = Quaternion.Euler(25, -35, 0);
            var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional;
            light.color = new Color(.88f, .92f, .96f); light.intensity = 1; light.cullingMask = ViewMask; light.shadows = LightShadows.None;
            ApplyIdlePose(); SetVisible(false);
        }
        private void OnDestroy() { CancelTouch(); RestoreSourceMask(); if (ViewCamera != null) Destroy(ViewCamera.gameObject); }
    }
}
