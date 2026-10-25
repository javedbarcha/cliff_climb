using System;
using UnityEngine;

namespace CanyonClimber
{
    // A surface-constrained pendulum + ballistic motor. Visual limbs never drive gameplay.
    // All movement advances at a fixed timestep; input/camera/presentation are separate.
    public sealed class ClimbMotor : MonoBehaviour
    {
        public enum ClimbState { Hanging, Reaching, Flying, Resting, Recovering, Complete }
        public ClimbSettings settings;
        public ClimbHold[] holds;
        public ClimbHold startHold;
        public ClimbState State { get; private set; }
        public Vector2 Body { get; private set; }
        public Vector2 Velocity { get; private set; }
        public Vector2 Aim { get; private set; }
        public bool Aiming { get; private set; }
        public bool Started { get; private set; }
        public bool Paused { get; private set; }
        public float Stamina { get; private set; } = 100f;
        public float BestHeight { get; private set; }
        public float Elapsed { get; private set; }
        public int Falls { get; private set; }
        public float SwingAngle => swing;
        public ClimbHold Anchor { get; private set; }
        public ClimbHold Checkpoint { get; private set; }
        public ClimbHold Target { get; private set; }
        public bool TargetIsReach { get; private set; }
        public float SlipRemaining => Mathf.Max(0f, settings.exhaustionGrace - exhaustedTime);
        public event Action<ClimbHold> Caught;
        public event Action Fell;
        float swing, angularSpeed, flightTime, exhaustedTime, recoverTime, reachTime;
        Vector2 reachFrom, reachTo;
        ClimbHold reachTarget, releasedHold;

        void Awake()
        {
            if (settings == null || startHold == null || holds == null || holds.Length == 0)
            { Debug.LogError("ClimbMotor needs settings, start hold and route.", this); enabled = false; return; }
            Checkpoint = startHold;
            Attach(startHold, false);
        }
        public void Begin() { Started = true; }
        public void SetPause(bool value) { Paused = value; CancelAim(); }
        public void SetAim(Vector2 value)
        {
            if (!Started || Paused || State != ClimbState.Hanging && State != ClimbState.Resting) return;
            Aiming = true; Aim = Vector2.ClampMagnitude(value, 1f);
            UpdatePrediction();
        }
        public void CancelAim() { Aiming = false; Aim = Vector2.zero; Target = null; }
        public Vector3 World(Vector2 coordinate, float radius = -1f)
        {
            float a = coordinate.x / settings.surfaceRadius;
            float r = radius < 0f ? settings.surfaceRadius : radius;
            return new Vector3(-Mathf.Sin(a) * r, coordinate.y, Mathf.Cos(a) * r);
        }
        public Vector3 Normal(float arc)
        {
            float a = arc / settings.surfaceRadius;
            return new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
        }
        public float NearestArc(float arc, float reference)
        {
            float c = settings.surfaceRadius * 2f * Mathf.PI;
            return reference + Mathf.Repeat(arc - reference + c * .5f, c) - c * .5f;
        }
        public Vector2 HoldNear(ClimbHold h, float reference)
        { return new Vector2(NearestArc(h.arc, reference), h.height); }
        public Vector2 HandPosition(Vector2 body) => body + Vector2.up * settings.armLength;
        public Vector2 LaunchVelocity(Vector2 aim)
        {
            float strength = Mathf.Clamp01(aim.magnitude);
            Vector2 dir = strength > .01f ? aim.normalized : Vector2.up;
            float tired = Mathf.Lerp(.65f, 1f, Mathf.Clamp01(Stamina / 25f));
            return Velocity * settings.momentumCarry + new Vector2(
                dir.x * settings.horizontalImpulse,
                dir.y * settings.verticalImpulse) * Mathf.Lerp(.4f, 1f, strength) * tired;
        }
        public void Commit()
        {
            if (!Aiming) return;
            UpdatePrediction();
            if (Aim.magnitude < .15f || State == ClimbState.Flying || State == ClimbState.Reaching)
            { CancelAim(); return; }
            if (Target != null && TargetIsReach)
            {
                reachTarget = Target;
                reachFrom = Body;
                reachTo = HoldNear(Target, Body.x) - Vector2.up * settings.armLength;
                reachTime = 0f; State = ClimbState.Reaching;
                Stamina = Mathf.Max(0, Stamina - settings.reachCost);
            }
            else
            {
                Velocity = LaunchVelocity(Aim);
                flightTime = 0f; State = ClimbState.Flying; releasedHold = Anchor; Anchor = null;
                Stamina = Mathf.Max(0, Stamina - settings.jumpCost);
            }
            CancelAim();
        }
        void FixedUpdate()
        {
            if (!Started || Paused || !enabled || State == ClimbState.Complete) return;
            float dt = Time.fixedDeltaTime;
            Elapsed += dt;
            switch (State)
            {
                case ClimbState.Hanging: TickSwing(dt); break;
                case ClimbState.Resting:
                    Velocity = Vector2.zero;
                    Stamina = Mathf.Min(100, Stamina + settings.restRecovery * dt);
                    exhaustedTime = 0f;
                    break;
                case ClimbState.Reaching:
                    reachTime += dt;
                    float t = Mathf.Clamp01(reachTime / settings.reachDuration);
                    Body = Vector2.Lerp(reachFrom, reachTo, t * t * (3f - 2f * t));
                    if (t >= 1f) Attach(reachTarget, false);
                    break;
                case ClimbState.Flying: TickFlight(dt); break;
                case ClimbState.Recovering:
                    recoverTime -= dt;
                    if (recoverTime <= 0f) ReturnToCheckpoint();
                    break;
            }
            BestHeight = Mathf.Max(BestHeight, Body.y);
            transform.position = World(Body);
            transform.rotation = Quaternion.LookRotation(-Normal(Body.x), Vector3.up);
            if (Aiming) UpdatePrediction();
        }
        void TickSwing(float dt)
        {
            float input = Aiming ? Aim.x : 0f;
            float accel = -settings.gravity / settings.armLength * Mathf.Sin(swing)
                + input * settings.pumpAcceleration - angularSpeed * settings.swingDamping;
            angularSpeed = Mathf.Clamp(angularSpeed + accel * dt, -settings.maxAngularSpeed, settings.maxAngularSpeed);
            swing += angularSpeed * dt;
            if (Mathf.Abs(swing) > settings.maxSwingAngle)
            { swing = Mathf.Sign(swing) * settings.maxSwingAngle; angularSpeed *= -.25f; }
            Vector2 a = HoldNear(Anchor, Body.x);
            Body = a + new Vector2(Mathf.Sin(swing), -Mathf.Cos(swing)) * settings.armLength;
            Velocity = new Vector2(Mathf.Cos(swing), Mathf.Sin(swing)) * settings.armLength * angularSpeed;
            Stamina = Mathf.Max(0f, Stamina - (settings.hangingDrain + Mathf.Abs(input) * settings.pumpingDrain) * dt);
            if (Stamina <= 0f)
            {
                exhaustedTime += dt;
                if (exhaustedTime >= settings.exhaustionGrace)
                { releasedHold = Anchor; Anchor = null; State = ClimbState.Flying; flightTime = 0f; CancelAim(); }
            }
            else exhaustedTime = 0f;
        }
        void TickFlight(float dt)
        {
            Vector2 oldHand = HandPosition(Body);
            Body += Velocity * dt + Vector2.down * (.5f * settings.gravity * dt * dt);
            Velocity += Vector2.down * (settings.gravity * dt);
            flightTime += dt;
            if (flightTime >= settings.catchDelay && flightTime <= settings.fallRecoverySeconds)
            {
                ClimbHold best = null; float closest = float.MaxValue;
                Vector2 hand = HandPosition(Body);
                foreach (var h in holds)
                {
                    if (h == null || (h == releasedHold && flightTime < .45f)) continue;
                    Vector2 p = HoldNear(h, Body.x);
                    if (Vector2.Distance(p, oldHand) > 3f && Vector2.Distance(p, hand) > 3f) continue;
                    float distance = DistanceToSegment(p, oldHand, hand);
                    if (distance < settings.catchRadius && distance < closest) { closest = distance; best = h; }
                }
                if (best != null) { Attach(best, true); return; }
            }
            if (Body.y < 1.1f || flightTime > settings.fallRecoverySeconds)
            {
                State = ClimbState.Recovering; recoverTime = .65f; Falls++;
                Velocity = Vector2.zero; Fell?.Invoke();
            }
        }
        public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude < .00001f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }
        void Attach(ClimbHold h, bool preserveMomentum)
        {
            Vector2 incoming = Velocity;
            Anchor = h;
            Body = HoldNear(h, Body.x) - Vector2.up * settings.armLength;
            swing = 0f;
            angularSpeed = preserveMomentum ? Mathf.Clamp(incoming.x / settings.armLength * .55f, -2.5f, 2.5f) : 0f;
            Velocity = Vector2.zero;
            State = h.rest ? ClimbState.Resting : ClimbState.Hanging;
            if (h.rest) { Checkpoint = h; exhaustedTime = 0f; }
            // Small catch relief permits a last-second save, without fully refilling stamina.
            if (preserveMomentum) { Stamina = Mathf.Min(100, Stamina + 4f); exhaustedTime = 0f; }
            if (h.summit) State = ClimbState.Complete;
            transform.position = World(Body);
            transform.rotation = Quaternion.LookRotation(-Normal(Body.x), Vector3.up);
            Caught?.Invoke(h);
        }
        public void ReturnToCheckpoint()
        {
            CancelAim(); Stamina = 100f; exhaustedTime = 0f;
            Attach(Checkpoint != null ? Checkpoint : startHold, false);
        }
        public void RestartRun()
        {
            Checkpoint = startHold; Elapsed = 0f; Falls = 0; BestHeight = 0f;
            ReturnToCheckpoint(); Paused = false;
        }
        public int Predict(Vector2[] points, out ClimbHold predicted, out bool isReach)
        {
            predicted = null; isReach = false;
            if (Aim.magnitude < .15f) return 0;
            // Nearby targets are selected by directional intent, measured from the supporting grip.
            if (Anchor != null)
            {
                float bestScore = -1f;
                Vector2 origin = HoldNear(Anchor, Body.x);
                foreach (var h in holds)
                {
                    if (h == null || h == Anchor) continue;
                    Vector2 delta = HoldNear(h, Body.x) - origin;
                    float distance = delta.magnitude;
                    if (distance > settings.reachDistance || distance < .05f) continue;
                    float alignment = Vector2.Dot(delta.normalized, Aim.normalized);
                    float score = alignment - distance * .035f;
                    if (alignment > .78f && score > bestScore) { bestScore = score; predicted = h; }
                }
                if (predicted != null)
                {
                    isReach = true;
                    points[0] = HandPosition(Body);
                    points[1] = HoldNear(predicted, Body.x);
                    return 2;
                }
            }
            Vector2 pos = Body, velocity = LaunchVelocity(Aim);
            int count = Mathf.Min(points.Length, 71);
            const float step = .02f; // Same integrator and timestep as the shipped physics settings.
            for (int i = 0; i < count; i++)
            {
                Vector2 previous = HandPosition(pos);
                points[i] = previous;
                if (i * step >= settings.catchDelay)
                {
                    foreach (var h in holds)
                    {
                        if (h == null || (h == Anchor && i * step < .45f)) continue;
                        Vector2 p = HoldNear(h, pos.x);
                        if (DistanceToSegment(p, i > 0 ? points[i - 1] : previous, previous) < settings.catchRadius)
                        { predicted = h; return i + 1; }
                    }
                }
                pos += velocity * step + Vector2.down * (.5f * settings.gravity * step * step);
                velocity += Vector2.down * (settings.gravity * step);
            }
            return count;
        }
        void UpdatePrediction()
        {
            Predict(predictionBuffer, out ClimbHold h, out bool reach);
            Target = h; TargetIsReach = reach;
        }
        readonly Vector2[] predictionBuffer = new Vector2[71];
    }
}
