using UnityEngine;

namespace CanyonClimber
{
    public sealed class ClimbHold : MonoBehaviour
    {
        public int id;
        [Tooltip("Distance around the cliff, not degrees.")]
        public float arc;
        public float height;
        public bool rest;
        public bool summit;
        public Vector2 Coordinate => new Vector2(arc, height);
    }
}
