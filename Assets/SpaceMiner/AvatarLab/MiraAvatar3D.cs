using UnityEngine;

namespace SpaceMiner
{
    public sealed class MiraAvatar3D : MonoBehaviour
    {
        public MiraAvatar Driver;
        public SkinnedMeshRenderer Face;
        public SkinnedMeshRenderer Teeth;
        public Transform Bust;
        public Transform HairDetail;
        public RenderTexture Portrait;
        private float nextBlink = 3.5f, blinkStart = -10;
        private float yaw;
        public int AnimatedFrames { get; private set; }

        private void LateUpdate()
        {
            if (Face == null || Driver == null) return;
            if (Input.GetMouseButton(0) && Input.mousePosition.x < Screen.width * .41f)
                yaw = Mathf.Clamp(yaw + Input.GetAxis("Mouse X") * 3, -50, 50);
            if (Input.GetKeyDown(KeyCode.R)) yaw = 0;
            if (Time.unscaledTime >= nextBlink)
            {
                blinkStart = Time.unscaledTime;
                nextBlink = Time.unscaledTime + Random.Range(3.4f, 5.5f);
            }
            float blink = Driver.Motion ? Mathf.Sin(Mathf.Clamp01((Time.unscaledTime - blinkStart) / .2f) * Mathf.PI) : 0;
            float mouth = Driver.Mouth;
            if (HairDetail != null) HairDetail.localRotation = Quaternion.identity;
            Set("JawOpen", mouth * 38);
            if(Teeth!=null){int jaw=Teeth.sharedMesh.GetBlendShapeIndex("JawOpen");if(jaw>=0)Teeth.SetBlendShapeWeight(jaw,mouth*38);}
            Set("Round", mouth * (20 + 15 * Mathf.Sin(Time.unscaledTime * 11)));
            Set("BlinkLeft", blink * 100); Set("BlinkRight", blink * 100);
            if (mouth > .07f) AnimatedFrames++;
            if (Bust != null) Bust.localRotation = Quaternion.Euler(Driver.Motion ? Mathf.Sin(Time.unscaledTime * .7f) * .6f : 0,
                yaw + (Driver.Motion ? Mathf.Sin(Time.unscaledTime * .4f) : 0), 0);
        }
        private void Set(string name, float weight)
        {
            int index = Face.sharedMesh.GetBlendShapeIndex(name);
            if (index >= 0) Face.SetBlendShapeWeight(index, weight);
        }
    }
}
