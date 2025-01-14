using Domain.Physics.CollisionUtils;
using UnityEngine;

namespace Domain.Physics
{
    /// <summary>
    /// Provides advanced swept collision detection for a moving sphere against a moving oriented box.
    /// </summary>
    public static class SweptBoxCollisionPro
    {
        /// <summary>
        /// Performs a swept collision test between a moving sphere and a moving oriented box.
        /// Returns true if a collision within [0, 1] is found.
        /// The collision point and normal are in world space.
        /// </summary>
        /// <param name="sphereStart">Starting position of the sphere.</param>
        /// <param name="sphereEnd">Ending position of the sphere.</param>
        /// <param name="sphereRadius">Radius of the sphere.</param>
        /// <param name="boxStart">Starting state of the box.</param>
        /// <param name="boxEnd">Ending state of the box.</param>
        /// <param name="collisionPoint">Collision point in world space, if detected.</param>
        /// <param name="collisionNormal">Collision normal in world space, if detected.</param>
        /// <param name="timeOfImpact">Time of impact within the [0, 1] interval, if detected.</param>
        /// <returns>True if a collision is detected; otherwise, false.</returns>
        public static bool SweptSphereToMovingOrientedBox(
            Vector3 sphereStart, Vector3 sphereEnd, float sphereRadius,
            OrientedBox boxStart, OrientedBox boxEnd,
            out Vector3 collisionPoint,
            out Vector3 collisionNormal,
            out float timeOfImpact)
        {
            collisionPoint = Vector3.zero;
            collisionNormal = Vector3.zero;
            timeOfImpact = 0f;

            // Compute relative motion
            Vector3 sphereVelocity = sphereEnd - sphereStart;
            Vector3 boxVelocity = boxEnd.Center - boxStart.Center;
            Vector3 relativeVelocity = sphereVelocity - boxVelocity;

            // Transform sphere positions to boxStart local space
            Matrix4x4 boxStartTransform = Matrix4x4.TRS(boxStart.Center, boxStart.Rotation, Vector3.one);
            Matrix4x4 invBoxStartTransform = boxStartTransform.inverse;

            Vector3 localSphereStart = invBoxStartTransform.MultiplyPoint3x4(sphereStart);
            Vector3 localSphereEnd = invBoxStartTransform.MultiplyPoint3x4(sphereStart + relativeVelocity);

            // Expanded box in local space
            Vector3 expandedBoxMin = -boxStart.HalfExtents - Vector3.one * sphereRadius;
            Vector3 expandedBoxMax = boxStart.HalfExtents + Vector3.one * sphereRadius;

            // Perform ray vs AABB intersection
            Vector3 localSphereDir = localSphereEnd - localSphereStart;

            if (RayAABBIntersection(localSphereStart, localSphereDir, expandedBoxMin, expandedBoxMax, out float tEnter, out Vector3 localNormal))
            {
                timeOfImpact = Mathf.Clamp01(tEnter);

                // Collision point in local space
                Vector3 localCollisionPoint = localSphereStart + localSphereDir * timeOfImpact - localNormal * sphereRadius;

                // Transform back to world space
                collisionPoint = boxStartTransform.MultiplyPoint3x4(localCollisionPoint);
                collisionNormal = boxStartTransform.MultiplyVector(localNormal).normalized;

                return true;
            }

            return false;
        }

        private static bool RayAABBIntersection(Vector3 rayOrigin, Vector3 rayDir, Vector3 boxMin, Vector3 boxMax, out float tEnter, out Vector3 normal)
        {
            float tEnterCandidate = -Mathf.Infinity;
            float tExitCandidate = Mathf.Infinity;
            Vector3 candidateNormal = Vector3.zero;
            Vector3 invDir = new Vector3(
                1f / (rayDir.x != 0 ? rayDir.x : Mathf.Epsilon),
                1f / (rayDir.y != 0 ? rayDir.y : Mathf.Epsilon),
                1f / (rayDir.z != 0 ? rayDir.z : Mathf.Epsilon)
            );

            for (int i = 0; i < 3; i++)
            {
                float t1 = (GetComponent(boxMin, i) - GetComponent(rayOrigin, i)) * GetComponent(invDir, i);
                float t2 = (GetComponent(boxMax, i) - GetComponent(rayOrigin, i)) * GetComponent(invDir, i);

                if (t1 > t2)
                {
                    (t1, t2) = (t2, t1);
                }

                if (t1 > tEnterCandidate)
                {
                    tEnterCandidate = t1;
                    candidateNormal = Vector3.zero;
                    SetComponent(ref candidateNormal, i, GetComponent(rayDir, i) > 0 ? -1f : 1f);
                }

                if (t2 < tExitCandidate)
                {
                    tExitCandidate = t2;
                }

                if (tEnterCandidate > tExitCandidate)
                {
                    tEnter = tEnterCandidate;
                    normal = candidateNormal;
                    return false;
                }
            }

            tEnter = tEnterCandidate;
            normal = candidateNormal;
            return true;
        }

        private static float GetComponent(Vector3 vector, int index)
        {
            return index switch
            {
                0 => vector.x,
                1 => vector.y,
                2 => vector.z,
                _ => 0f
            };
        }

        private static void SetComponent(ref Vector3 vector, int index, float value)
        {
            switch (index)
            {
                case 0:
                    vector.x = value;
                    break;
                case 1:
                    vector.y = value;
                    break;
                case 2:
                    vector.z = value;
                    break;
            }
        }
    }
}