using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpaceMiner
{
    public enum MiraVisualMode { Image, LowModel, HighModel }

    // A model-specific rig rendered only into this module's portrait texture.
    // Its surfaces are never enabled during world-camera rendering.
    [DefaultExecutionOrder(300)]
    public sealed class MiraPortraitView : MonoBehaviour
    {
        public MiraVisualMode Mode { get; private set; }
        public MiraAvatar3D ActiveAvatar { get; private set; }
        public MiraFacePerformance Performance { get; private set; }
        public RenderTexture Texture { get; private set; }
        public bool HeadMotion = true, EyeMotion = true, Expressions = true;
        public bool BodyMotion = true;
        public float MovementStrength = 1;
        public float ExpressionCoupling = 1;
        public MiraDialogueActing Acting;
        public float MouthStrength = 1;
        public bool Visible;
        public bool PreviewWhenClosed;
        public AudioSource Speech;
        public MiraAvatar.Mood Mood;
        private readonly Dictionary<MiraVisualMode, GameObject> instances = new Dictionary<MiraVisualMode, GameObject>();
        private Camera portraitCamera;
        private Light[] portraitLights;
        private Texture2D image;
        private static int instanceNumber;
        private Vector3 origin;

        public void SetMode(MiraVisualMode mode)
        {
            if (Mode == mode && (mode == MiraVisualMode.Image || ActiveAvatar != null)) return;
            if (mode != MiraVisualMode.Image && mode != MiraVisualMode.LowModel && mode != MiraVisualMode.HighModel)
                throw new ArgumentOutOfRangeException(nameof(mode));
            if (ActiveAvatar != null) ActiveAvatar.gameObject.SetActive(false);
            ActiveAvatar = null; Performance = null;
            if (mode != MiraVisualMode.Image)
            {
                if (!instances.TryGetValue(mode, out var root))
                {
                    string path = "MiraModels/" + (mode == MiraVisualMode.HighModel ? "High" : "Low");
                    var prefab = Resources.Load<GameObject>(path);
                    if (prefab == null) throw new InvalidOperationException("Mira model prefab missing: " + path);
                    root = Instantiate(prefab);
                    root.name = mode == MiraVisualMode.HighModel ? "Mira Tripo 3.1 portrait" : "Mira Tripo 2.5 portrait";
                    root.hideFlags = HideFlags.DontSave;
                    if (origin == Vector3.zero) origin = new Vector3(-100 - 5 * ++instanceNumber, -100, 0);
                    root.transform.position = origin;
                    instances.Add(mode, root);
                }
                root.SetActive(true);
                ActiveAvatar = root.GetComponent<MiraAvatar3D>();
                // Portrait input belongs to the host, not the old standalone demo's drag driver.
                ActiveAvatar.enabled = false;
                ActiveAvatar.Driver.Speech = Speech;
                ActiveAvatar.Face.enabled = false;
                Performance = root.GetComponent<MiraFacePerformance>();
                if (portraitCamera == null) CreateCamera();
                if (Texture != null) { Texture.Release(); Destroy(Texture); }
                int size = mode == MiraVisualMode.HighModel ? 1024 : 512;
                Texture = new RenderTexture(size, size, 24) { name = "Mira " + mode + " portrait", antiAliasing = 4 };
                Texture.Create(); portraitCamera.targetTexture = Texture;
            }
            Mode = mode;
        }

        private void CreateCamera()
        {
            var root = new GameObject("Mira portrait rendering") { hideFlags = HideFlags.DontSave };
            root.transform.SetParent(transform, false);
            portraitCamera = root.AddComponent<Camera>();
            portraitCamera.enabled = false;
            portraitCamera.orthographic = true; portraitCamera.orthographicSize = .335f;
            portraitCamera.nearClipPlane = .1f; portraitCamera.farClipPlane = 3;
            portraitCamera.cullingMask = 1 << 27;
            portraitCamera.clearFlags = CameraClearFlags.SolidColor;
            portraitCamera.backgroundColor = new Color(.025f, .045f, .075f);
            portraitCamera.transform.position = origin + new Vector3(0, .34f, 1.5f);
            portraitCamera.transform.LookAt(origin + new Vector3(0, .34f, 0));
            portraitLights = new Light[3];
            Vector3[] directions = { new Vector3(-.7f,.9f,1), new Vector3(.8f,.6f,.9f), new Vector3(.5f,.8f,-1) };
            Color[] colors = { new Color(1,.92f,.85f), new Color(.72f,.86f,1), new Color(.28f,.75f,1) };
            float[] strengths = { 1.05f, .45f, .7f };
            for (int i = 0; i < 3; i++)
            {
                var light = new GameObject("Mira portrait light " + i).AddComponent<Light>();
                light.transform.SetParent(root.transform, false); light.transform.position = origin + directions[i];
                light.transform.LookAt(origin + new Vector3(0, .34f, 0));
                light.type = LightType.Directional; light.color = colors[i]; light.intensity = strengths[i];
                light.cullingMask = 1 << 27; light.shadows = LightShadows.None; light.enabled = false;
                portraitLights[i] = light;
            }
        }

        private void LateUpdate()
        {
            if (ActiveAvatar == null) return;
            bool active = Visible || PreviewWhenClosed;
            if (ActiveAvatar.gameObject.activeSelf != active) ActiveAvatar.gameObject.SetActive(active);
            if (!active) return;
            ActiveAvatar.Driver.SetMood(Mood); ActiveAvatar.Driver.Motion = HeadMotion;
            ActiveAvatar.Driver.Sensitivity = 12 * Mathf.Clamp(MouthStrength, .4f, 1.4f);
            Performance.EyeMotion = EyeMotion; Performance.Expressions = Expressions;
            Performance.Acting = Acting;
            Performance.BodyMotion = BodyMotion;
            Performance.MovementStrength = Mathf.Clamp(MovementStrength, 0, 1.5f);
            Performance.ExpressionCoupling = Mathf.Clamp01(ExpressionCoupling);
            Render();
        }

        public void Render()
        {
            if (ActiveAvatar == null || portraitCamera == null) return;
            // The host may be a moving camera. Keep the offscreen camera and its
            // child lights fixed relative to the model, independent of host motion.
            portraitCamera.transform.position = origin + new Vector3(0, .34f, 1.5f);
            portraitCamera.transform.LookAt(origin + new Vector3(0, .34f, 0));
            // Scope the portrait lighting so station sunlight/fog never changes Mira's face.
            var ambientMode = RenderSettings.ambientMode; var ambientLight = RenderSettings.ambientLight;
            bool fog = RenderSettings.fog;
            var otherLights = new List<Light>(); var masks = new List<int>();
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (Array.IndexOf(portraitLights, light) >= 0 || (light.cullingMask & (1 << 27)) == 0) continue;
                otherLights.Add(light); masks.Add(light.cullingMask); light.cullingMask &= ~(1 << 27);
            }
            try
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.36f,.36f,.38f); RenderSettings.fog = false;
                foreach (var light in portraitLights) light.enabled = true;
                ActiveAvatar.Face.enabled = true;
                portraitCamera.Render();
            }
            finally
            {
                ActiveAvatar.Face.enabled = false;
                foreach (var light in portraitLights) light.enabled = false;
                for (int i = 0; i < otherLights.Count; i++) if (otherLights[i] != null) otherLights[i].cullingMask = masks[i];
                RenderSettings.ambientMode = ambientMode; RenderSettings.ambientLight = ambientLight; RenderSettings.fog = fog;
            }
        }

        public void Draw(Rect bounds)
        {
            if (Mode != MiraVisualMode.Image && Texture != null)
                GUI.DrawTexture(bounds, Texture, ScaleMode.ScaleToFit, true);
            else
            {
                if (image == null) image = Resources.Load<Texture2D>("Mira/MiraDialoguePortraits");
                if (image == null) return;
                int cell = (int)Mood;
                GUI.DrawTextureWithTexCoords(bounds, image, new Rect(cell % 2 * .5f, cell < 2 ? .5f : 0, .5f, .5f), true);
            }
        }
        private void OnDisable()
        {
            Visible = false;
            foreach (var instance in instances.Values) if (instance != null) instance.SetActive(false);
        }
        private void OnDestroy()
        {
            foreach (var instance in instances.Values) if (instance != null) Destroy(instance);
            if (Texture != null) { Texture.Release(); Destroy(Texture); }
        }
    }
}
