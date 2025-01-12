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
            // Create a root object for the ball
            GameObject ball = new GameObject("TableTennisBall");
            ball.layer = LayerMask.NameToLayer("Ball");

            // Create a child object for the ball visuals
            GameObject ballVisuals = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballVisuals.name = "BallVisuals";
            ballVisuals.transform.SetParent(ball.transform, false);
            ballVisuals.transform.localScale = Vector3.one * _config.Ball.DiameterMeters;

            // Remove the collider from the visuals
            Object.Destroy(ballVisuals.GetComponent<SphereCollider>());

            // Add a SphereCollider to the root object
            SphereCollider sphereCollider = ball.AddComponent<SphereCollider>();
            sphereCollider.radius = _config.Ball.DiameterMeters * 0.5f;
            sphereCollider.material = _config.Ball.Material;

            // Add a Rigidbody to the root object
            Rigidbody rb = ball.AddComponent<Rigidbody>();
            rb.mass = _config.Ball.MassKg;
            rb.linearDamping = 0f; // Minimal air drag handled by custom physics
            rb.angularDamping = 0f; // Spin damping also handled by custom physics
            rb.useGravity = false; // TODO: True when pure unity physics?
            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Assign a basic material (color) or custom texture to the ball
            Renderer renderer = ballVisuals.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material ballMat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = Color.white // Basic white ball
                };
                renderer.material = ballMat;
            }

            // Create a child object to serve as a “marking ring” for spin visibility
            CreateSpinRing(ball, new Vector3(90f, 0f, 0f));
            CreateSpinRing(ball, Vector3.zero);

            return ball;
        }

        /// <summary>
        /// Creates a visual spin ring around the ball for better spin visibility.
        /// </summary>
        /// <param name="parentBall">The parent ball GameObject to attach the spin ring to.</param>
        /// <param name="rotation">The rotation of the spin ring.</param>
        private void CreateSpinRing(GameObject parentBall, Vector3 rotation)
        {
            // The ring is a thin “cylinder” placed around the ball’s equator
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "SpinRing";
            ring.transform.SetParent(parentBall.transform, false);

            // Scale the ring:
            //  - X, Z = ball diameter + small offset (to sit just outside the sphere)
            //  - Y = small thickness
            float ballDiameter = parentBall.GetComponent<SphereCollider>().radius * 2f;
            float ringDiameter = ballDiameter + 0.0005f; // Slightly bigger than the sphere
            float ringThickness = 0.001f; // Thickness of the ring “belt”

            // Cylinder in Unity is aligned along Y-axis, so scale (X=diameter, Y=height, Z=diameter).
            ring.transform.localScale = new Vector3(
                ringDiameter,
                ringThickness,
                ringDiameter
            );
            // Rotate the ring by 90° so that it wraps horizontally. E.g.:
            ring.transform.localEulerAngles = rotation;

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