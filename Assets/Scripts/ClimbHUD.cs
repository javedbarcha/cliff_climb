using UnityEngine;

namespace CanyonClimber
{
    public sealed class ClimbHUD : MonoBehaviour
    {
        public ClimbMotor motor;
        public ClimbInput input;
        GUIStyle title, heading, body, small, button;
        readonly Color ink = new Color(.045f,.12f,.18f,.94f);
        readonly Color mint = new Color(.28f,.96f,.65f);
        float width, height;
        void Awake() { Application.targetFrameRate = 60; Screen.sleepTimeout = SleepTimeout.NeverSleep; }
        void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 31, fontStyle = FontStyle.Bold };
            heading = new GUIStyle(title) { fontSize = 21 };
            body = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            small = new GUIStyle(body) { fontSize = 13 };
            button = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
            title.normal.textColor = heading.normal.textColor = body.normal.textColor = small.normal.textColor = Color.white;
        }
        static void Panel(Rect r, Color color)
        { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }
        void Text(float x, float y, float w, float h, string text, GUIStyle style) => GUI.Label(new Rect(x,y,w,h), text, style);
        void OnGUI()
        {
            if (motor == null || motor.settings == null) return;
            Styles();
            float scale = Mathf.Min(Screen.width / 420f, Screen.height / 800f);
            Rect safe = Screen.safeArea;
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(safe.x, Screen.height-safe.yMax,0), Quaternion.identity, Vector3.one * scale);
            width = safe.width / scale; height = safe.height / scale;
            Panel(new Rect(12,12,width-24,106), ink);
            Text(25,19,240,37,"CANYON CLIMBER",heading);
            Text(25,49,200,28,$"HEIGHT {Mathf.Max(0,motor.Body.y):0.0} m",heading);
            Text(25,79,210,25,$"{motor.Elapsed:0.0}s  /  FALLS {motor.Falls}",small);
            if (GUI.Button(new Rect(width-91,25,65,34),motor.Paused ? "PLAY" : "PAUSE",button))
                motor.SetPause(!motor.Paused);
            if (GUI.Button(new Rect(width-91,69,65,32),"RESET",button)) motor.ReturnToCheckpoint();
            Panel(new Rect(24,126,width-48,13),new Color(.05f,.12f,.16f,.8f));
            Panel(new Rect(24,126,(width-48)*motor.Stamina/100f,13),motor.Stamina < 25 ? new Color(1,.45f,.2f) : mint);
            Text(24,143,width-48,25,$"STAMINA {motor.Stamina:0}%" + (motor.State == ClimbMotor.ClimbState.Resting ? "  /  RESTING" : ""),small);
            if (!motor.Started) Intro();
            else if (motor.State == ClimbMotor.ClimbState.Complete) Complete();
            else if (motor.Paused)
            {
                Panel(new Rect(30,height*.4f,width-60,114),ink);
                Text(48,height*.4f+15,width-96,36,"TAKE A BREATHER",heading);
                Text(48,height*.4f+53,width-96,50,"Tap PLAY to continue your climb.",body);
            }
            else
            {
                string message = "Drag below to aim. Release to move.";
                if (motor.State == ClimbMotor.ClimbState.Flying) message = "REACHING!  Hands catch nearby holds automatically.";
                if (motor.State == ClimbMotor.ClimbState.Recovering) message = "Back to your last resting ledge...";
                if (motor.State == ClimbMotor.ClimbState.Resting) message = "Safe ledge! Recover, then drag upward to climb.";
                if (motor.Aiming && motor.Aim.magnitude >= .15f)
                    message = motor.Target == null ? "! NO CATCH PREDICTED - release to risk a jump" : motor.TargetIsReach ? "+ REACH - release to transfer your grip" : "> JUMP - catch predicted at current momentum";
                if (motor.Stamina <= 0 && motor.Anchor != null && !motor.Anchor.rest)
                    message = $"SLIPPING! {motor.SlipRemaining:0.0}s - reach a safe hold!";
                Panel(new Rect(12,height-108,width-24,94),ink);
                Text(26,height-100,width-52,44,message,body);
                Text(26,height-55,width-52,36,"Pump left/right with the swing. Return thumb to its start to cancel.",small);
                if (input.Dragging)
                {
                    Vector2 o = new Vector2((input.Origin.x-safe.x)/scale,(Screen.height-safe.yMax + safe.height-input.Origin.y)/scale);
                    // Convert directly from bottom-left screen coordinates into safe-area GUI coordinates.
                    o.y = (safe.yMax-input.Origin.y)/scale;
                    Vector2 p = o + new Vector2(motor.Aim.x,-motor.Aim.y)*56f;
                    Panel(new Rect(o.x-28,o.y-28,56,56),new Color(.05f,.2f,.25f,.5f));
                    Panel(new Rect(p.x-9,p.y-9,18,18),mint);
                }
            }
            GUI.matrix = previous;
        }
        void Intro()
        {
            float y = Mathf.Max(186,height*.3f);
            Panel(new Rect(22,y,width-44,350),ink);
            Text(42,y+18,width-84,45,"ONE MORE HOLD",title);
            Text(42,y+67,width-84,175,"Climb around the canyon tower.\n\nDrag to aim; release to reach or jump.\nPump left/right in rhythm for bigger leaps.\nGreen: reach. Cyan: jump. Orange: risk.\nRest on turquoise ledges to save progress.",body);
            if (GUI.Button(new Rect(42,y+275,width-84,50),"LET'S CLIMB",button)) motor.Begin();
        }
        void Complete()
        {
            float y = height*.35f;
            Panel(new Rect(25,y,width-50,220),ink);
            Text(46,y+15,width-92,45,"SUMMIT REACHED!",heading);
            Text(46,y+65,width-92,65,$"Time: {motor.Elapsed:0.0}s\nFalls: {motor.Falls}  /  Best height: {motor.BestHeight:0.0}m",body);
            if (GUI.Button(new Rect(46,y+145,width-92,48),"CLIMB AGAIN",button)) motor.RestartRun();
        }
    }
}
