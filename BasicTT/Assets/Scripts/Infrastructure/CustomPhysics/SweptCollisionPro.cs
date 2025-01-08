using UnityEngine;

namespace Infrastructure.CustomPhysics
{
    public static class SweptBoxCollisionPro
    {
        /// <summary>
        /// Performs a swept collision test between a moving sphere (start->end) and an oriented box.
        /// Returns true if a collision within [0, 1] is found. 
        /// The collision point and normal are in *world space*.
        /// 
        /// This approach:
        /// 1. Transforms the sphere start/end into the box's local space (making the box an AABB).
        /// 2. Expands that AABB by the sphereRadius.
        /// 3. Does a standard Ray-vs-AABB parametric intersection.
        /// 4. Finds the earliest time t in [0,1] the sphere enters the expanded box.
        /// 5. Transforms the local collision point & normal back to world space.
        /// 
        /// Note: 
        ///  - This does not handle box rotation during dt, only translation.
        ///  - If sphere starts *inside* the expanded box, we treat timeOfImpact=0.
        /// </summary>
        public static bool SweptSphereToOrientedBox(
            Vector3 sphereStart, Vector3 sphereEnd, float sphereRadius,
            BoxCollider boxCollider,
            out Vector3 collisionPoint,
            out Vector3 collisionNormal,
            out float timeOfImpact)
        {
            collisionPoint = Vector3.zero;
            collisionNormal = Vector3.zero;
            timeOfImpact = 0f;

            // 1) Extract box info
            Transform boxTransform = boxCollider.transform;
            Vector3 boxLocalCenter = boxCollider.center;
            Vector3 worldBoxCenter = boxTransform.TransformPoint(boxLocalCenter);
            Quaternion boxRotation = boxTransform.rotation;
            Vector3 boxScale = boxTransform.lossyScale;
            Vector3 boxSize = Vector3.Scale(boxCollider.size, boxScale);
            Vector3 halfSize = boxSize * 0.5f;

            // 2) Compute "local space" transform for the box
            Matrix4x4 m = Matrix4x4.TRS(worldBoxCenter, boxRotation, boxScale);
            Matrix4x4 invM = m.inverse;

            Vector3 sphereStartLocal = invM.MultiplyPoint3x4(sphereStart);
            Vector3 sphereEndLocal = invM.MultiplyPoint3x4(sphereEnd);

            Vector3 localBoxHalfSize = boxCollider.size * 0.5f;

            // 3) Expand the box by sphereRadius
            Vector3 expand = new Vector3(sphereRadius, sphereRadius, sphereRadius);
            Vector3 minB = -localBoxHalfSize - expand;
            Vector3 maxB = localBoxHalfSize + expand;

            // 4) Check if the sphere starts inside the box
            bool startsInside = IsPointInsideAABB(sphereStartLocal, minB, maxB);
            if (startsInside)
            {
                timeOfImpact = 0f;
                collisionNormal = GetLocalNormalFromInsidePoint(sphereStartLocal, minB, maxB);
                collisionPoint = ClampPointInAABB(sphereStartLocal, minB, maxB);
                collisionPoint = m.MultiplyPoint3x4(collisionPoint);
                collisionNormal = (m.MultiplyVector(collisionNormal)).normalized;
                return true;
            }

            // 5) Perform Ray-vs-AABB intersection
            Vector3 dirLocal = sphereEndLocal - sphereStartLocal;
            float tEnter = 0f;
            float tExit = 1f;
            int enterAxis = -1;
            bool success = true;

            for (int i = 0; i < 3; i++)
            {
                float startVal = (i == 0) ? sphereStartLocal.x : (i == 1) ? sphereStartLocal.y : sphereStartLocal.z;
                float dirVal = (i == 0) ? dirLocal.x : (i == 1) ? dirLocal.y : dirLocal.z;
                float minVal = (i == 0) ? minB.x : (i == 1) ? minB.y : minB.z;
                float maxVal = (i == 0) ? maxB.x : (i == 1) ? maxB.y : maxB.z;

                if (Mathf.Abs(dirVal) < 1e-8f)
                {
                    if (startVal < minVal || startVal > maxVal)
                    {
                        success = false;
                        break;
                    }
                }
                else
                {
                    float invDir = 1f / dirVal;
                    float t1 = (minVal - startVal) * invDir;
                    float t2 = (maxVal - startVal) * invDir;

                    float enterThisAxis = Mathf.Min(t1, t2);
                    float exitThisAxis = Mathf.Max(t1, t2);

                    if (enterThisAxis > tEnter)
                    {
                        tEnter = enterThisAxis;
                        enterAxis = i;
                    }
                    if (exitThisAxis < tExit)
                    {
                        tExit = exitThisAxis;
                    }

                    if (tEnter > tExit)
                    {
                        success = false;
                        break;
                    }
                }
            }

            if (!success || tEnter > 1f || tExit < 0f)
                return false;

            timeOfImpact = Mathf.Clamp(tEnter, 0f, 1f);
            Vector3 sphereCollisionCenterLocal = sphereStartLocal + dirLocal * timeOfImpact;
            Vector3 localNormal = Vector3.zero;

            if (enterAxis == 0)
            {
                localNormal = (dirLocal.x > 0f) ? new Vector3(-1f, 0f, 0f) : new Vector3(1f, 0f, 0f);
            }
            else if (enterAxis == 1)
            {
                localNormal = (dirLocal.y > 0f) ? new Vector3(0f, -1f, 0f) : new Vector3(0f, 1f, 0f);
            }
            else if (enterAxis == 2)
            {
                localNormal = (dirLocal.z > 0f) ? new Vector3(0f, 0f, -1f) : new Vector3(0f, 0f, 1f);
            }

            Vector3 localContactPoint = sphereCollisionCenterLocal - localNormal * sphereRadius;
            collisionPoint = m.MultiplyPoint3x4(localContactPoint);
            collisionNormal = (m.MultiplyVector(localNormal)).normalized;

            return true;
        }

        private static bool IsPointInsideAABB(Vector3 p, Vector3 minB, Vector3 maxB)
        {
            return (p.x >= minB.x && p.x <= maxB.x) &&
                   (p.y >= minB.y && p.y <= maxB.y) &&
                   (p.z >= minB.z && p.z <= maxB.z);
        }

        private static Vector3 GetLocalNormalFromInsidePoint(Vector3 p, Vector3 minB, Vector3 maxB)
        {
            float distToMinX = Mathf.Abs(p.x - minB.x);
            float distToMaxX = Mathf.Abs(maxB.x - p.x);
            float distToMinY = Mathf.Abs(p.y - minB.y);
            float distToMaxY = Mathf.Abs(maxB.y - p.y);
            float distToMinZ = Mathf.Abs(p.z - minB.z);
            float distToMaxZ = Mathf.Abs(maxB.z - p.z);

            float minDist = distToMinX;
            Vector3 normal = new Vector3(-1f, 0f, 0f);

            if (distToMaxX < minDist) { minDist = distToMaxX; normal = new Vector3(1f, 0f, 0f); }
            if (distToMinY < minDist) { minDist = distToMinY; normal = new Vector3(0f, -1f, 0f); }
            if (distToMaxY < minDist) { minDist = distToMaxY; normal = new Vector3(0f, 1f, 0f); }
            if (distToMinZ < minDist) { minDist = distToMinZ; normal = new Vector3(0f, 0f, -1f); }
            if (distToMaxZ < minDist) { normal = new Vector3(0f, 0f, 1f); }

            return normal;
        }

        private static Vector3 ClampPointInAABB(Vector3 p, Vector3 minB, Vector3 maxB)
        {
            return new Vector3(
                Mathf.Clamp(p.x, minB.x, maxB.x),
                Mathf.Clamp(p.y, minB.y, maxB.y),
                Mathf.Clamp(p.z, minB.z, maxB.z)
            );
        }
    }
}