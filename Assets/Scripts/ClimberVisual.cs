using UnityEngine;

namespace CanyonClimber
{
    // Articulates already-authored low-poly parts. No assets or meshes are created at runtime.
    public sealed class ClimberVisual : MonoBehaviour
    {
        public ClimbMotor motor;
        public Transform torso, head, backpack;
        public Transform upperArmL, forearmL, handL, upperArmR, forearmR, handR;
        public Transform thighL, shinL, bootL, thighR, shinR, bootR;
        Vector3 leftHand, rightHand;
        bool initialized;
        void LateUpdate()
        {
            if (motor == null) return;
            float time = motor.Elapsed;
            bool flying = motor.State == ClimbMotor.ClimbState.Flying || motor.State == ClimbMotor.ClimbState.Recovering;
            bool resting = motor.State == ClimbMotor.ClimbState.Resting || motor.State == ClimbMotor.ClimbState.Complete;
            float sway = flying ? Mathf.Sin(time * 9f) * 20f : -motor.SwingAngle * 16f;
            if (torso) torso.localRotation = Quaternion.Euler(0, 0, sway);
            if (head) head.localRotation = Quaternion.Euler(-6, Mathf.Sin(time * 2f) * 8f, sway * .5f);
            if (backpack) backpack.localRotation = Quaternion.Euler(5, 0, sway * .6f);
            Vector3 l, r;
            if (motor.Anchor != null && !resting)
            {
                Vector3 grip = transform.InverseTransformPoint(motor.World(motor.Anchor.Coordinate, motor.settings.holdRadius));
                l = grip + Vector3.left * .12f; r = grip + Vector3.right * .12f;
                if (motor.Aiming && motor.Target != null)
                {
                    Vector3 target = transform.InverseTransformPoint(motor.World(motor.Target.Coordinate, motor.settings.holdRadius));
                    if (target.x > 0) r = Vector3.Lerp(r, target, .24f); else l = Vector3.Lerp(l, target, .24f);
                }
            }
            else if (resting)
            { l = new Vector3(-.5f, -.15f, .05f); r = new Vector3(.5f, -.15f, .05f); }
            else
            {
                float wiggle = Mathf.Sin(time * 12f) * .1f;
                l = new Vector3(-.45f - wiggle, .95f, .14f);
                r = new Vector3(.45f + wiggle, .95f, .14f);
            }
            float blend = initialized ? 1f - Mathf.Exp(-22f * Time.deltaTime) : 1f;
            leftHand = Vector3.Lerp(leftHand, l, blend); rightHand = Vector3.Lerp(rightHand, r, blend);
            Arm(new Vector3(-.28f, .27f, 0), leftHand, -1, upperArmL, forearmL, handL);
            Arm(new Vector3(.28f, .27f, 0), rightHand, 1, upperArmR, forearmR, handR);
            float kick = flying ? Mathf.Sin(time * 10f) * .25f : Mathf.Sin(motor.SwingAngle * 2f) * .15f;
            Leg(-1, kick, resting, thighL, shinL, bootL);
            Leg(1, -kick, resting, thighR, shinR, bootR);
            initialized = true;
        }
        static void Segment(Transform part, Vector3 a, Vector3 b, float thickness)
        {
            if (part == null) return;
            part.localPosition = (a + b) * .5f;
            part.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
            part.localScale = new Vector3(thickness, Vector3.Distance(a, b), thickness);
        }
        static void Arm(Vector3 shoulder, Vector3 hand, int side, Transform upper, Transform lower, Transform palm)
        {
            Vector3 elbow = (shoulder + hand) * .5f + new Vector3(side * .18f, -.04f, -.12f);
            Segment(upper, shoulder, elbow, .16f); Segment(lower, elbow, hand, .13f);
            if (palm) { palm.localPosition = hand; palm.localScale = new Vector3(.2f,.17f,.19f); }
        }
        static void Leg(int side, float kick, bool resting, Transform thigh, Transform shin, Transform boot)
        {
            Vector3 hip = new Vector3(side * .18f, -.3f, 0);
            Vector3 knee = new Vector3(side * .28f, -.68f + kick, resting ? -.02f : .27f);
            Vector3 foot = new Vector3(side * .3f, -1.13f + kick, resting ? .05f : .13f);
            Segment(thigh, hip, knee, .2f); Segment(shin, knee, foot, .16f);
            if (boot) boot.localPosition = foot;
        }
    }
}
