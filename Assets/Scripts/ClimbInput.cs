using UnityEngine;

namespace CanyonClimber
{
    public sealed class ClimbInput : MonoBehaviour
    {
        public ClimbMotor motor;
        public bool Dragging { get; private set; }
        public Vector2 Origin { get; private set; }
        public Vector2 Pointer { get; private set; }
        int finger = -1;
        void Update()
        {
            if (!motor.Started || motor.Paused || motor.State == ClimbMotor.ClimbState.Complete)
            { Cancel(); return; }
            if (Input.touchCount > 0)
            {
                foreach (Touch t in Input.touches)
                {
                    if (t.phase == TouchPhase.Began && finger == -1 && !Dragging && t.position.y < Screen.height * .72f)
                    { finger = t.fingerId; Begin(t.position); }
                    if (t.fingerId != finger) continue;
                    if (t.phase == TouchPhase.Canceled) { Cancel(); }
                    else if (t.phase == TouchPhase.Ended) { Pointer = t.position; Move(); Release(); }
                    else { Pointer = t.position; Move(); }
                }
                return;
            }
            if (finger != -1) { Cancel(); return; }
            if (Input.GetMouseButtonDown(0) && Input.mousePosition.y < Screen.height * .72f) Begin(Input.mousePosition);
            if (Dragging && Input.GetMouseButton(0)) { Pointer = Input.mousePosition; Move(); }
            if (Dragging && Input.GetMouseButtonUp(0)) Release();
            if (Input.GetKeyDown(KeyCode.R)) motor.ReturnToCheckpoint();
        }
        void Begin(Vector2 p) { Dragging = true; Origin = Pointer = p; Move(); }
        void Move()
        {
            float scale = Mathf.Min(Screen.width / 420f, Screen.height / 900f);
            motor.SetAim((Pointer - Origin) / Mathf.Max(60f, motor.settings.aimPixels * scale));
        }
        void Release() { motor.Commit(); Dragging = false; finger = -1; }
        public void Cancel() { motor.CancelAim(); Dragging = false; finger = -1; }
        void OnApplicationPause(bool paused) { if (paused) { Cancel(); motor.SetPause(true); } }
        void OnApplicationFocus(bool focus) { if (!focus) { Cancel(); motor.SetPause(true); } }
    }
}
