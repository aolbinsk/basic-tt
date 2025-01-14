using UnityEngine;

namespace Domain.Physics.CollisionUtils
{
    /// <summary>
    /// Your OrientedBox struct; must match exactly how you interpret center/rotation/halfExtents
    /// so that local coords are [-he..+he] for each axis.
    /// </summary>
    public struct OrientedBox
    {
        public Vector3 Center;
        public Quaternion Rotation;
        public Vector3 HalfExtents;

        public OrientedBox(Vector3 center, Quaternion rotation, Vector3 halfExtents)
        {
            Center = center;
            Rotation = rotation;
            HalfExtents = halfExtents;
        }
    }
}