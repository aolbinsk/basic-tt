using Domain.Interfaces;
using UnityEngine;

namespace Infrastructure.SceneSetup
{
    /// <summary>
    /// Responsible for constructing the paddle GameObject with proper physics, visuals, and configuration.
    /// </summary>
    public class PaddleBuilder
    {
        private readonly IPhysicsConfig _config;

        /// <summary>
        /// Initializes a new instance of the PaddleBuilder class with the specified physics configuration.
        /// </summary>
        /// <param name="config">The physics configuration to use for paddle dimensions and materials.</param>
        public PaddleBuilder(IPhysicsConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Builds and configures a paddle GameObject.
        /// </summary>
        /// <returns>The constructed paddle GameObject.</returns>
        public GameObject BuildPaddle()
        {
            // 1) Create a parent GameObject to hold the entire paddle
            var paddleRoot = new GameObject("Paddle")
            {
                layer = LayerMask.NameToLayer("Paddle")
            };

            // 2) Build the paddle head geometry
            var paddleHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paddleHead.name = "PaddleHead";
            paddleHead.transform.SetParent(paddleRoot.transform, false);

            // 3) Scale the paddle head according to config
            paddleHead.transform.localScale = new Vector3(
                _config.Paddle.WidthMeters,
                _config.Paddle.ThicknessMeters,
                _config.Paddle.LengthMeters
            );

            // 4) Position the paddle head
            float halfLength = _config.Paddle.LengthMeters * 0.5f;
            paddleHead.transform.localPosition = new Vector3(0f, 0f, halfLength * 0.5f);

            // 5) Remove existing collider and add forehand and backhand side colliders
            Object.Destroy(paddleHead.GetComponent<BoxCollider>());

            // Forehand side collider
            var forehandSide = new GameObject("ForehandSide");
            forehandSide.transform.SetParent(paddleHead.transform, false);
            forehandSide.transform.localPosition = Vector3.zero;
            forehandSide.transform.localScale = Vector3.one;
            var forehandCollider = forehandSide.AddComponent<BoxCollider>();
            forehandCollider.size = new Vector3(0.5f, 1f, 1f);
            forehandCollider.center = new Vector3(-0.25f, 0f, 0f);
            forehandCollider.material = _config.Paddle.Material;

            // Backhand side collider
            var backhandSide = new GameObject("BackhandSide");
            backhandSide.transform.SetParent(paddleHead.transform, false);
            backhandSide.transform.localPosition = Vector3.zero;
            backhandSide.transform.localScale = Vector3.one;
            var backhandCollider = backhandSide.AddComponent<BoxCollider>();
            backhandCollider.size = new Vector3(0.5f, 1f, 1f);
            backhandCollider.center = new Vector3(0.25f, 0f, 0f);
            backhandCollider.material = _config.Paddle.Material;

            // 6) Configure the paddle head material
            var headRenderer = paddleHead.GetComponent<Renderer>();
            if (headRenderer != null)
            {
                var paddleMat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = Color.red
                };
                headRenderer.material = paddleMat;
            }

            // 7) Build the handle
            var paddleHandle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            paddleHandle.name = "PaddleHandle";
            paddleHandle.transform.SetParent(paddleRoot.transform, false);

            float handleRadius = _config.Paddle.HandleRadiusMeters;
            float handleLength = _config.Paddle.HandleLengthMeters;
            paddleHandle.transform.localScale = new Vector3(handleRadius * 2f, handleLength * 0.5f, handleRadius * 2f);

            float handleOffsetZ = halfLength + (handleLength * 0.5f);
            paddleHandle.transform.localPosition = new Vector3(0f, 0f, handleOffsetZ);
            paddleHandle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var handleCollider = paddleHandle.AddComponent<CapsuleCollider>();
            handleCollider.material = _config.Paddle.Material;

            return paddleRoot;
        }
    }
}