using UnityEngine;

namespace SpaceMiner
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [DefaultExecutionOrder(1100)] // Follow the final celestial camera pose in this frame.
    public sealed class Starfield : MonoBehaviour
    {
        public Camera View;

        private void LateUpdate()
        {
            if (View != null) transform.position = View.transform.position;
        }
    }
}
