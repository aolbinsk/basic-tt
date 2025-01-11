using UnityEngine;

namespace Domain.Physics
{
    /// <summary>
    /// Provides advanced swept collision detection for a moving sphere against an oriented box.
    /// </summary>
    public static class SweptBoxCollisionPro
    {
        /// <summary>
        /// Performs a swept collision test between a moving sphere (start->end) and an oriented box.
        /// Returns true if a collision within [0, 1] is found. 
        /// The collision point and normal are in *world space*.
        /// </summary>
        public static bool SweptSphereToOrientedBox(
            Vector3 sphereStart, Vector3 sphereEnd, float sphereRadius,
            OrientedBox box,
            out Vector3 collisionPoint,
            out Vector3 collisionNormal,
            out float timeOfImpact)
        {
            collisionPoint = Vector3.zero;
            collisionNormal = Vector3.zero;
            timeOfImpact = 0f;
            
            // 1) Transform the sphere's start and end positions into the box's local space
            Matrix4x4 boxTransform = Matrix4x4.TRS(box.Center, box.Rotation, Vector3.one);
            Matrix4x4 invBoxTransform = boxTransform.inverse;

            Vector3 localSphereStart = invBoxTransform.MultiplyPoint3x4(sphereStart);
            Vector3 localSphereEnd = invBoxTransform.MultiplyPoint3x4(sphereEnd);

            Vector3 localSphereDir = localSphereEnd - localSphereStart;

            // 2) Expand the box by the sphere's radius
            Vector3 expandedBoxMin = -box.HalfExtents - Vector3.one * sphereRadius;
            Vector3 expandedBoxMax = box.HalfExtents + Vector3.one * sphereRadius;

            // 3) Perform ray vs AABB intersection
            Vector3 rayDir = localSphereDir.normalized;
            float tEnter = 0f;
            float tExit = 1f;

            for (int i = 0; i < 3; i++)
            {
                float start = GetComponent(localSphereStart, i);
                float dir = GetComponent(localSphereDir, i);
                float minB = GetComponent(expandedBoxMin, i);
                float maxB = GetComponent(expandedBoxMax, i);

                if (Mathf.Approximately(dir, 0f))
                {
                    if (start < minB || start > maxB)
                        return false;
                }
                else
                {
                    float invDir = 1f / dir;
                    float t1 = (minB - start) * invDir;
                    float t2 = (maxB - start) * invDir;

                    if (t1 > t2)
                    {
                        float temp = t1;
                        t1 = t2;
                        t2 = temp;
                    }

                    if (t1 > tEnter)
                        tEnter = t1;
                    if (t2 < tExit)
                        tExit = t2;
                    if (tEnter > tExit)
                        return false;
                    if (tExit < 0f)
                        return false;
                }
            }

            timeOfImpact = Mathf.Clamp01(tEnter);

            Vector3 localCollisionPoint = localSphereStart + localSphereDir * timeOfImpact;
            Vector3 localNormal = GetCollisionNormal(localCollisionPoint, expandedBoxMin, expandedBoxMax);

            collisionPoint = boxTransform.MultiplyPoint3x4(localCollisionPoint - localNormal * sphereRadius);
            collisionNormal = boxTransform.MultiplyVector(localNormal).normalized;

            return true;
        }

        private static float GetComponent(Vector3 vector, int index)
        {
            switch (index)
            {
                case 0: return vector.x;
                case 1: return vector.y;
                case 2: return vector.z;
                default: return 0f;
            }
        }

        private static Vector3 GetCollisionNormal(Vector3 point, Vector3 minB, Vector3 maxB)
        {
            float dx = Mathf.Min(point.x - minB.x, maxB.x - point.x);
            float dy = Mathf.Min(point.y - minB.y, maxB.y - point.y);
            float dz = Mathf.Min(point.z - minB.z, maxB.z - point.z);

            float minDist = Mathf.Min(dx, Mathf.Min(dy, dz));

            if (minDist == dx)
                return new Vector3(point.x > 0 ? 1f : -1f, 0f, 0f);
            else if (minDist == dy)
                return new Vector3(0f, point.y > 0 ? 1f : -1f, 0f);
            else
                return new Vector3(0f, 0f, point.z > 0 ? 1f : -1f);
        }
    }
}