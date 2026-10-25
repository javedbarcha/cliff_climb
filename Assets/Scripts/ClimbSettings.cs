using UnityEngine;

namespace CanyonClimber
{
    [CreateAssetMenu(menuName = "Canyon Climber/Movement settings")]
    public sealed class ClimbSettings : ScriptableObject
    {
        [Header("Cliff surface (metres)")]
        public float surfaceRadius = 6.6f;
        public float holdRadius = 6.0f;
        public float armLength = 1.15f;
        [Header("Supported movement")]
        public float reachDistance = 1.5f;
        public float reachDuration = .3f;
        [Header("Physical swing")]
        public float gravity = 9.8f;
        public float pumpAcceleration = 7.2f;
        public float swingDamping = .65f;
        public float maxSwingAngle = 1.1f;
        public float maxAngularSpeed = 4f;
        [Header("Jump / automatic catch")]
        public float horizontalImpulse = 4.7f;
        public float verticalImpulse = 6.1f;
        public float momentumCarry = .7f;
        public float catchRadius = .48f;
        public float catchDelay = .12f;
        public float fallRecoverySeconds = 2.4f;
        [Header("Stamina")]
        public float hangingDrain = .55f;
        public float pumpingDrain = 1.1f;
        public float jumpCost = 6f;
        public float reachCost = 1f;
        public float restRecovery = 25f;
        public float exhaustionGrace = 3f;
        public float aimPixels = 105f;
    }
}
