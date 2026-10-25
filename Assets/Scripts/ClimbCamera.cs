using UnityEngine;

namespace CanyonClimber
{
    public sealed class ClimbCamera : MonoBehaviour
    {
        public ClimbMotor motor;
        public float distance = 12f;
        public float lookAbove = 2f;
        public float followSpeed = 9f;
        Vector3 smoothPosition;
        bool initialized;
        void LateUpdate()
        {
            if (motor == null) return;
            Vector3 body = motor.World(motor.Body);
            float orbitArc = motor.Anchor != null ? motor.Anchor.arc : motor.Body.x;
            Vector3 pivot = motor.World(new Vector2(orbitArc, motor.Body.y));
            Vector3 desired = pivot + motor.Normal(orbitArc) * distance + Vector3.up * 2.7f;
            float a = initialized ? 1f - Mathf.Exp(-followSpeed * Time.deltaTime) : 1f;
            smoothPosition = Vector3.Lerp(smoothPosition, desired, a);
            transform.position = smoothPosition;
            transform.rotation = Quaternion.LookRotation(body + Vector3.up * lookAbove - smoothPosition, Vector3.up);
            initialized = true;
        }
    }
}
