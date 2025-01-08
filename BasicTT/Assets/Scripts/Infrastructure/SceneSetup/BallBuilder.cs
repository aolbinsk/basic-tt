using Domain.Interfaces;
using UnityEngine;
using Infrastructure.Config;

namespace Infrastructure.SceneSetup
{
    /// <summary>
    /// Responsible for constructing the ball GameObject with proper physics, visuals, and configuration.
    /// </summary>
    public class BallBuilder
    {
        private readonly IPhysicsConfig _config;

        /// <summary>
        /// Initializes a new instance of the BallBuilder class with the specified physics configuration.
        /// </summary>
        /// <param name="config">The physics configuration to use for the ball.</param>
        public BallBuilder(IPhysicsConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Builds and configures the ball GameObject.
        /// </summary>
        /// <returns>The constructed ball GameObject.</returns>
        public GameObject BuildBall()
        {
            // 1) Create a sphere primitive for the ball
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "TableTennisBall";
            ball.layer = LayerMask.NameToLayer("Ball");

            // 2) Scale the sphere to match the ball diameter
            float ballDiameterMeters = _config.Ball.DiameterMeters;
            ball.transform.localScale = Vector3.one * ballDiameterMeters;

            // 3) Add a Rigidbody for physics
            Rigidbody rb = ball.AddComponent<Rigidbody>();
            rb.mass = _config.Ball.MassKg;
            rb.linearDamping = 0f; // Minimal air drag handled by custom physics
            rb.angularDamping = 0f; // Spin damping also handled by custom physics
            rb.useGravity = false; // TODO: True when pure unity physics?
            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // 4) Assign a bouncy or custom physics material to the SphereCollider
            SphereCollider sphereCollider = ball.GetComponent<SphereCollider>();
            sphereCollider.material = _config.Ball.Material;

            // 5) Assign a basic material (color) or custom texture to the ball
            Renderer renderer = ball.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material ballMat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = Color.white // Basic white ball
                };
                renderer.material = ballMat;
            }

            // 6) Create a child object to serve as a “marking ring” for spin visibility
            CreateSpinRing(ball);

            return ball;
        }

        /// <summary>
        /// Creates a visual spin ring around the ball for better spin visibility.
        /// </summary>
        /// <param name="parentBall">The parent ball GameObject to attach the spin ring to.</param>
        private void CreateSpinRing(GameObject parentBall)
        {
            // The ring is a thin “cylinder” placed around the ball’s equator
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "SpinRing";
            ring.transform.SetParent(parentBall.transform, false);

            // Scale the ring:
            //  - X, Z = ball diameter + small offset (to sit just outside the sphere)
            //  - Y = small thickness
            float ballDiameter = parentBall.transform.localScale.x;
            float ringRadius = ballDiameter * 0.51f; // Slightly bigger than the sphere’s radius
            float ringThickness = 0.02f; // Thickness of the ring “belt”

            // Cylinder in Unity is aligned along Y-axis, so scale (X=diameter, Y=height, Z=diameter).
            ring.transform.localScale = new Vector3(
                ringRadius,
                ringThickness,
                ringRadius
            );

            // Position so that the ring is around the equator
            ring.transform.localPosition = Vector3.zero;

            // Change the ring color
            Renderer ringRenderer = ring.GetComponent<Renderer>();
            if (ringRenderer != null)
            {
                Material ringMat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = Color.black // Distinct color
                };
                ringRenderer.material = ringMat;
            }

            // Remove the collider on the ring, as it is only needed for visual spin indication
            Collider ringCollider = ring.GetComponent<Collider>();
            if (ringCollider != null) Object.Destroy(ringCollider);
        }
    }
}