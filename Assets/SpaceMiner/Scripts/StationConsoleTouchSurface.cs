using UnityEngine;

namespace SpaceMiner
{
    // A physical patch of glass, expressed in its own moving console's space.
    // It is deliberately separate from UI pixels and the floor-level E prompt.
    public sealed class StationConsoleTouchSurface : MonoBehaviour
    {
        public Vector3 LocalOrigin, LocalNormal = Vector3.up, LocalForward = Vector3.forward;
        public Vector2 HalfSize = new Vector2(.4f, .3f);
        public TouchConsoleVisual ConsoleVisual { get; private set; }
        public Vector3 Normal => transform.TransformDirection(LocalNormal).normalized;
        public Vector3 Forward => Vector3.ProjectOnPlane(transform.TransformDirection(LocalForward), Normal).normalized;
        public Vector3 Origin => transform.TransformPoint(LocalOrigin);
        public Transform ConsoleRoot => ConsoleVisual != null ? ConsoleVisual.transform.parent
            : GetComponentInParent<StationTerminal>() != null ? GetComponentInParent<StationTerminal>().transform : transform;
        public bool VisibleFrom(Camera view)
        {
            if (!TryGetTouchPoint(view,out var point,out var normal)) return false;
            return !Physics.Linecast(view.transform.position,point+normal*.004f,out var hit,1<<25,QueryTriggerInteraction.Ignore)
                || hit.transform.IsChildOf(ConsoleRoot);
        }
        public float ViewScore(Camera view)
        {
            if(!TryGetTouchPoint(view,out var point,out _))return float.PositiveInfinity;
            Vector3 delta=point-view.transform.position;
            return delta.magnitude+.20f*(1-Vector3.Dot(view.transform.forward,delta.normalized));
        }

        public void Configure(TouchConsoleVisual visual, Vector3 origin, Vector3 normal, Vector3 forward, Vector2 halfSize)
        { ConsoleVisual = visual; LocalOrigin = origin; LocalNormal = normal.normalized; LocalForward = forward.normalized; HalfSize = halfSize; }

        public bool TryGetTouchPoint(Camera view, out Vector3 point, out Vector3 normal)
        {
            point = Origin; normal = Normal;
            if (view == null || !isActiveAndEnabled) return false;
            Vector3 forward = Forward, right = Vector3.Cross(normal, forward).normalized;
            Vector3 delta = view.transform.position - point;
            // Keep three pads comfortably within the bezel. Choosing the nearest
            // part of the panel lets a wide central console work from either side.
            // The right arm uses the nearer right-hand patch, without crossing
            // the player's view to hit the mathematical centre of the screen.
            point += right * Mathf.Clamp(Vector3.Dot(delta, right)+.20f, -HalfSize.x, HalfSize.x)
                + forward * Mathf.Clamp(Vector3.Dot(delta, forward), -HalfSize.y, HalfSize.y);
            return Vector3.Dot(view.transform.position - point, normal) > .02f;
        }

        public static StationConsoleTouchSurface Find(Transform interaction)
        {
            if (interaction == null) return null;
            var direct = interaction.GetComponentInChildren<StationConsoleTouchSurface>();
            if (direct != null) return direct;
            var block = interaction.GetComponentInParent<CommandRoomBlock>();
            return block != null ? block.GetComponentInChildren<StationConsoleTouchSurface>()
                : interaction.parent != null ? interaction.parent.GetComponentInChildren<StationConsoleTouchSurface>() : null;
        }
    }
}
