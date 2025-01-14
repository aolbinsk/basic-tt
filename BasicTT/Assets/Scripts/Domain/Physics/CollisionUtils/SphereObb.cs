namespace Domain.Physics.CollisionUtils
{
    using UnityEngine;

    public static class SphereObb
    {
        public static bool SweptSphereToObb(
            Vector3 sphereStartWorld,
            Vector3 sphereEndWorld,
            float sphereRadius,
            OrientedBox boxStart,
            OrientedBox boxEnd,
            out Vector3 collisionPoint,
            out Vector3 collisionNormal,
            out float timeOfImpact)
        {
            collisionPoint = Vector3.zero;
            collisionNormal = Vector3.zero;
            timeOfImpact = 0f;

            // Relative velocity
            Vector3 sphereVel = (sphereEndWorld - sphereStartWorld);
            Vector3 boxVel = (boxEnd.Center - boxStart.Center);
            Vector3 relativeVel = sphereVel - boxVel;

            // Inverse transform to local coords of boxStart
            Matrix4x4 M = Matrix4x4.TRS(boxStart.Center, boxStart.Rotation, Vector3.one);
            Matrix4x4 invM = M.inverse;

            Vector3 localSphereStart = invM.MultiplyPoint3x4(sphereStartWorld);
            // End in local coords => start + relativeVel (still in world). We want start + local(RelVel).
            // Actually we can do: localSphereEnd = invM.MultiplyPoint3x4(sphereStartWorld + relativeVel).
            // Then localDir = localSphereEnd - localSphereStart.
            // but let's do it in two steps:
            Vector3 localSphereEnd = invM.MultiplyPoint3x4(sphereStartWorld + relativeVel);

            Vector3 localDir = localSphereEnd - localSphereStart;

            // Expand the box by sphereRadius => new min/max
            Vector3 he = boxStart.HalfExtents;
            Vector3 minAABB = -he - Vector3.one * sphereRadius;
            Vector3 maxAABB = he + Vector3.one * sphereRadius;

            float tHit;
            Vector3 localN;
            bool hit = CollisionUtils.RayAABBIntersection(
                localSphereStart, localDir, minAABB, maxAABB, out tHit, out localN);
            if (!hit) return false;

            // If you only want collisions within this fraction step, tHit must be in [0..1].
            // The RayAABB code already enforces that. So we know tHit is valid.

            // Convert local collision normal back to world
            collisionNormal = M.MultiplyVector(localN).normalized;

            // timeOfImpact is tHit
            timeOfImpact = tHit;

            // Collision point in local coords
            Vector3 localHitPos = localSphereStart + localDir * tHit;
            collisionPoint = M.MultiplyPoint3x4(localHitPos);

            return true;
        }
    }
}