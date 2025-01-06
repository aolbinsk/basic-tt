using UnityEngine;

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
        Vector3 boxLocalCenter  = boxCollider.center;             // center in the box's local coords
        Vector3 worldBoxCenter  = boxTransform.TransformPoint(boxLocalCenter);
        Quaternion boxRotation  = boxTransform.rotation;
        Vector3 boxScale        = boxTransform.lossyScale;
        // Actual world size of the box
        Vector3 boxSize = Vector3.Scale(boxCollider.size, boxScale);
        Vector3 halfSize = boxSize * 0.5f;

        // 2) We assume the box moves from (worldBoxCenter) at t=0 to (worldBoxCenter + boxVelocity * dt) by t=1
        //    If your box is truly dynamic, you'd pass the boxStart / boxEnd or velocity separately.
        //    For now, let's assume the box is effectively static OR we only care about the box's position at this instant.
        //    If the box is moving, you can adjust the sphereStart/sphereEnd or do a relative motion approach. 
        //    For a typical VR table tennis paddle, you might pass the paddle velocity and do a "relative" motion:
        // sphereStartRel = sphereStart - boxVelocity * dt, etc.

        // For demonstration, let’s treat the box as static during dt:
        // (If you want a moving box, see notes below about relative motion.)

        // 3) Compute "local space" transform for the box. We'll invert the box transform 
        //    so that in local space, the box is AABB in [-halfSize, +halfSize].
        //    Then we transform the sphere's start/end points into that space.
        Matrix4x4 invBoxMatrix = Matrix4x4.TRS(worldBoxCenter, boxRotation, Vector3.one).inverse;
        // Or you can build it from boxTransform worldToLocalMatrix, but if there's scaling, we have to be careful:
        // In many cases, boxTransform.worldToLocalMatrix might do the trick,
        // but let's do it explicitly for clarity:
        //   - We want: localPos = invBoxRot * (worldPos - worldBoxCenter)

        // Actually, to handle scale, we can do:
        //   Matrix4x4 m = Matrix4x4.TRS(worldBoxCenter, boxRotation, boxScale);
        //   Matrix4x4 invBoxMatrix = m.inverse;
        Matrix4x4 m = Matrix4x4.TRS(worldBoxCenter, boxRotation, boxScale);
        Matrix4x4 invM = m.inverse;

        Vector3 sphereStartLocal = invM.MultiplyPoint3x4(sphereStart);
        Vector3 sphereEndLocal   = invM.MultiplyPoint3x4(sphereEnd);

        // The local box is now effectively an AABB from (-halfSize.x, -halfSize.y, -halfSize.z) to (+halfSize.x, +halfSize.y, +halfSize.z).
        // But in local coords (after factoring out scale) the halfSize is boxCollider.size * 0.5f, 
        // since the scale has been accounted for in the transform. 
        // Actually, after the above transform, the box is scaled to a 1x1 box, but let's do it systematically:

        Vector3 localBoxHalfSize = (boxCollider.size * 0.5f); // because we used the matrix with "boxScale"

        // 4) Expand the box by sphereRadius => we get an "expanded AABB"
        //    min = -localBoxHalfSize - sphereRadius
        //    max = +localBoxHalfSize + sphereRadius
        Vector3 expand = new Vector3(sphereRadius, sphereRadius, sphereRadius);
        Vector3 minB   = -localBoxHalfSize - expand;
        Vector3 maxB   =  localBoxHalfSize + expand;

        // 5) We do a parametric intersection of the line (sphereStartLocal + t * dirLocal) with that expanded AABB.
        Vector3 dirLocal = sphereEndLocal - sphereStartLocal;

        // We'll handle the case if the sphere starts *inside* the box:
        bool startsInside = IsPointInsideAABB(sphereStartLocal, minB, maxB);
        if (startsInside)
        {
            // Immediate collision at t=0
            timeOfImpact = 0f;
            // For collision normal, we can find the nearest face or fallback to a direction of (startLocal - center) 
            // or something. We'll do the nearest face approach:
            collisionNormal = GetLocalNormalFromInsidePoint(sphereStartLocal, minB, maxB);
            // We'll compute collisionPoint in local space as sphereStartLocal, 
            // or push it to the boundary in the direction of collisionNormal if needed.
            Vector3 boundary = ClampPointInAABB(sphereStartLocal, minB, maxB);
            // "boundary" is effectively the nearest point on the AABB to sphereStartLocal
            collisionPoint = boundary; // local space

            // Transform back to world space:
            collisionPoint  = m.MultiplyPoint3x4(collisionPoint);
            collisionNormal = (m.MultiplyVector(collisionNormal)).normalized;
            return true;
        }

        // If the sphere is outside, we do the standard "Ray vs. AABB" approach:
        // We'll find the param range [tEnter, tExit] for each axis, then take the intersection of those ranges.
        // If tEnter <= tExit and that intersection overlaps with [0,1], we have a collision.

        // For each axis (x, y, z), compute t1, t2 where the line intersects minB[i] / maxB[i].
        // We'll track the largest tEnter and smallest tExit among x, y, z.
        float tEnter = 0f;
        float tExit  = 1f;

        // We also want to remember which axis gave us the final tEnter to deduce collision normal sign.
        int enterAxis = -1; // 0=x,1=y,2=z
        bool success  = true;

        for (int i = 0; i < 3; i++)
        {
            float startVal = (i == 0) ? sphereStartLocal.x : (i == 1) ? sphereStartLocal.y : sphereStartLocal.z;
            float dirVal   = (i == 0) ? dirLocal.x : (i == 1) ? dirLocal.y : dirLocal.z;
            float minVal   = (i == 0) ? minB.x : (i == 1) ? minB.y : minB.z;
            float maxVal   = (i == 0) ? maxB.x : (i == 1) ? maxB.y : maxB.z;

            if (Mathf.Abs(dirVal) < 1e-8f)
            {
                // No real motion along this axis. If we're outside minVal..maxVal => no intersection
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
                float exitThisAxis  = Mathf.Max(t1, t2);

                // Track global tEnter / tExit
                if (enterThisAxis > tEnter)
                {
                    tEnter = enterThisAxis;
                    enterAxis = i; // the axis giving the final "enter" 
                }
                if (exitThisAxis < tExit)
                {
                    tExit = exitThisAxis;
                }

                // If at any point tEnter > tExit, no intersection
                if (tEnter > tExit)
                {
                    success = false;
                    break;
                }
            }
        }

        if (!success) 
            return false; // No intersection or parallel miss

        // Now we have a candidate range [tEnter, tExit]. If tEnter > 1 or tExit < 0 => no overlap in [0,1].
        if (tEnter > 1f || tExit < 0f)
            return false;

        // The earliest collision time in [0,1] is clamp(tEnter, 0, 1).
        float tCollide = Mathf.Clamp(tEnter, 0f, 1f);
        if (tCollide > 1f) 
            return false; // collision happens after dt
        if (tCollide < 0f) 
            return false; // collision happened before dt (we might handle as time=0 if we want)

        timeOfImpact = tCollide;

        // 6) Compute local collision point 
        Vector3 sphereCollisionCenterLocal = sphereStartLocal + dirLocal * tCollide;
        // That’s the sphere center at collision. We still need the exact "contact point" on the box boundary
        // or on the sphere surface. Typically, you'd want the contact on the sphere's surface or on the box's face.

        // The collision normal: we know the axis that gave us the tEnter. 
        // If dirVal > 0 => we collided with the "min face", else the "max face."
        float collisionDirVal = (enterAxis == 0) ? dirLocal.x : (enterAxis == 1) ? dirLocal.y : dirLocal.z;
        // If collisionDirVal > 0, we came from negative side => normal points -X, if axis=0, etc.
        Vector3 localNormal = Vector3.zero;
        if (enterAxis == 0)
        {
            localNormal = (collisionDirVal > 0f) ? new Vector3(-1f, 0f, 0f) : new Vector3(1f, 0f, 0f);
        }
        else if (enterAxis == 1)
        {
            localNormal = (collisionDirVal > 0f) ? new Vector3(0f, -1f, 0f) : new Vector3(0f, 1f, 0f);
        }
        else if (enterAxis == 2)
        {
            localNormal = (collisionDirVal > 0f) ? new Vector3(0f, 0f, -1f) : new Vector3(0f, 0f, 1f);
        }

        // To get a local contact point on the box surface (not just sphere center):
        // we clamp the center of the sphere to the *inner* side of the expanded box, 
        // but for a more realistic contact point, subtract the sphere radius along localNormal:
        // i.e. localContactPoint = sphereCollisionCenterLocal - localNormal * sphereRadius
        // Then clamp that to the actual box face, because we might be colliding near an edge/corner:
        // However, for a pro-level ping-pong feel, it's often enough to just offset the sphere center by sphereRadius:
        Vector3 localContactPoint = sphereCollisionCenterLocal - localNormal * sphereRadius;

        // 7) Transform back to world space
        collisionPoint  = m.MultiplyPoint3x4(localContactPoint);
        collisionNormal = (m.MultiplyVector(localNormal)).normalized; // transform normal, then normalize

        return true;
    }

    /// <summary>
    /// Checks if point p is inside the AABB defined by minB & maxB.
    /// </summary>
    private static bool IsPointInsideAABB(Vector3 p, Vector3 minB, Vector3 maxB)
    {
        return (p.x >= minB.x && p.x <= maxB.x) &&
               (p.y >= minB.y && p.y <= maxB.y) &&
               (p.z >= minB.z && p.z <= maxB.z);
    }

    /// <summary>
    /// If the sphere starts inside the expanded box, we can pick a normal from the closest box face 
    /// or corner. A simpler method is to see which axis we're closest to the boundary.
    /// </summary>
    private static Vector3 GetLocalNormalFromInsidePoint(Vector3 p, Vector3 minB, Vector3 maxB)
    {
        // We'll compute the distance to each face and pick whichever is smallest.
        // Then pick the sign for the normal. 
        // This is a rough approach but usually enough for a quick "inside" normal direction.
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
        if (distToMaxZ < minDist) { /* minDist = distToMaxZ; */ normal = new Vector3(0f, 0f, 1f); }

        return normal;
    }

    /// <summary>
    /// Clamps a point p to the AABB [minB, maxB]. 
    /// Returns the point in the box that's closest to p.
    /// </summary>
    private static Vector3 ClampPointInAABB(Vector3 p, Vector3 minB, Vector3 maxB)
    {
        return new Vector3(
            Mathf.Clamp(p.x, minB.x, maxB.x),
            Mathf.Clamp(p.y, minB.y, maxB.y),
            Mathf.Clamp(p.z, minB.z, maxB.z)
        );
    }
}
