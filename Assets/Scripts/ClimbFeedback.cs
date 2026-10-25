using UnityEngine;

namespace CanyonClimber
{
    public sealed class ClimbFeedback : MonoBehaviour
    {
        public ClimbMotor motor;
        public LineRenderer trajectory, targetRing;
        readonly Vector2[] points = new Vector2[71];
        void LateUpdate()
        {
            bool visible = motor.Started && !motor.Paused && motor.Aiming && motor.Aim.magnitude >= .15f;
            trajectory.enabled = visible; targetRing.enabled = visible && motor.Target != null;
            if (!visible) return;
            int count = motor.Predict(points, out ClimbHold target, out bool reach);
            Color color = target == null ? new Color(1f,.38f,.22f) : reach ? new Color(.35f,1f,.5f) : new Color(.1f,.9f,1f);
            trajectory.startColor = trajectory.endColor = color;
            trajectory.positionCount = count;
            for (int i = 0; i < count; i++) trajectory.SetPosition(i, motor.World(points[i], motor.settings.surfaceRadius + .15f));
            if (target == null) return;
            targetRing.startColor = targetRing.endColor = color;
            targetRing.positionCount = 25;
            float pulse = reach ? 1f : 1f + Mathf.Sin(Time.unscaledTime * 7f) * .12f;
            Vector3 center = motor.World(target.Coordinate, motor.settings.holdRadius + .38f);
            Vector3 tangent = Vector3.Cross(Vector3.up, -motor.Normal(target.arc));
            for (int i = 0; i < 25; i++)
            {
                float a = i / 24f * Mathf.PI * 2f;
                targetRing.SetPosition(i, center + (tangent * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)) * (.39f * pulse));
            }
        }
    }
}
