namespace Domain.Physics.CollisionUtils
{
    using UnityEngine;

    public static class CollisionUtils
    {
        /// <summary>
        /// Tests a ray (parametric from <paramref name="rayOrigin"/> in direction <paramref name="rayDir"/>) against
        /// an axis-aligned box defined by <paramref name="boxMin"/> and <paramref name="boxMax"/>.
        /// If an intersection occurs, <c>tEnter</c> is set to the param where the ray enters the box, 
        /// and <c>hitNormal</c> is the outward normal of the face hit first (never zero).
        ///
        /// Returns <c>true</c> if there's an intersection for t >= 0, otherwise false.
        /// </summary>
        public static bool RayAABBIntersection(Vector3 origin, Vector3 dir, Vector3 boxMin, Vector3 boxMax, 
            out float tEnter, out Vector3 normal)
        {
            tEnter = 0f;
            normal = Vector3.zero;
            float tNear = float.NegativeInfinity;
            float tFar  = float.PositiveInfinity;
            int   axis  = -1;
            float axisSign = 0f;

            // 1. For each axis:
            for (int i=0; i<3; i++)
            {
                float o = GetComponent(origin, i);
                float d = GetComponent(dir, i);
                float minI = GetComponent(boxMin, i);
                float maxI = GetComponent(boxMax, i);

                if (Mathf.Abs(d)<1e-12f)
                {
                    // Ray parallel. If outside slab => no intersection
                    if (o<minI || o>maxI) return false;
                    continue;
                }
                float t1=(minI - o)/d, t2=(maxI - o)/d;
                bool swapped=false;
                if (t1>t2) { (t1,t2)=(t2,t1); swapped=true; }

                if (t1>tNear)
                {
                    tNear   = t1;
                    axis    = i;
                    axisSign= (swapped ^ (d<0f)) ? 1f : -1f;
                }
                if (t2<tFar) 
                    tFar = t2;
                if (tNear>tFar) return false;
            }

            // 2. If tNear < 0 => inside start => treat as no collision or immediate t=0 as you wish.
            if (tNear<0f) return false;

            // 3. If you only want collisions that happen within the sub-step => ensure tNear <= 1
            if (tNear>1f) return false;

            // We have an intersection in [0..1].
            tEnter = tNear;
            switch (axis)
            {
                case 0: normal= new Vector3(axisSign,0,0); break;
                case 1: normal= new Vector3(0,axisSign,0); break;
                case 2: normal= new Vector3(0,0,axisSign); break;
            }
            return true;
        }

        /// <summary>
        /// Utility to fetch the x/y/z component by index.
        /// </summary>
        private static float GetComponent(Vector3 v, int index)
        {
            return (index == 0) ? v.x : (index == 1) ? v.y : v.z;
        }
    }
}