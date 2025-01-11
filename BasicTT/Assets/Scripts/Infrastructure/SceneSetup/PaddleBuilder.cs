using Domain.Interfaces;
using UnityEngine;

namespace Infrastructure.SceneSetup
{
    /// <summary>
    /// Responsible for constructing the paddle GameObject with proper physics, visuals, and configuration.
    /// Builds paddle in standard orientation: head centered at origin, handle along -X, forehand facing +Z.
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
                _config.Paddle.HeadLengthMeters, // Along z-axis
                _config.Paddle.HeadThicknessMeters, // Along y-axis
                _config.Paddle.HeadWidthMeters // Along x-axis
            );

            // 4) Position the paddle head to align with the handle center
            float halfLength = _config.Paddle.HeadLengthMeters * 0.5f;
            paddleHead.transform.localPosition = new Vector3(0f, 0f, -halfLength * 0.5f);

            // 5) Adjust the hit zones
            SetupHitZones(paddleHead);

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

            // Align handle along the z-axis, centered under the paddle head
            float handleOffsetZ = -halfLength - (handleLength * 0.5f);
            paddleHandle.transform.localPosition = new Vector3(0f, 0f, handleOffsetZ);
            paddleHandle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Align along z-axis

            // 8) Configure the handle material
            var handleRenderer = paddleHandle.GetComponent<Renderer>();
            if (handleRenderer != null)
            {
                var handleMat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = Color.gray
                };
                handleRenderer.material = handleMat;
            }

            return paddleRoot;
        }

        private void SetupHitZones(GameObject paddleHead)
        {
            // Suppose the entire paddle thickness is 0.02f (2cm).
            // We'll create two child objects: "ForehandSide" and "BackhandSide"
            // Each will have half the thickness (0.01f).

            float halfThickness = _config.Paddle.HeadThicknessMeters * 0.5f;
            float fullWidth     = _config.Paddle.HeadWidthMeters;
            float fullLength    = _config.Paddle.HeadLengthMeters;

            // 1) Forehand side
            var forehandZone = new GameObject("ForehandSide");
            forehandZone.transform.SetParent(paddleHead.transform, false);

            // Position the forehand collider so its center is 1/2 of the halfThickness away from the paddle center:
            forehandZone.transform.localPosition = new Vector3(0f, 0f, +halfThickness * 0.5f); 
            // Or whichever axis is "forward"

            var forehandCollider = forehandZone.AddComponent<BoxCollider>();
            forehandCollider.size = new Vector3(fullWidth, _config.Paddle.HeadThicknessMeters * 0.5f, fullLength);

            // 2) Backhand side
            var backhandZone = new GameObject("BackhandSide");
            backhandZone.transform.SetParent(paddleHead.transform, false);

            // Position the backhand collider so its center is –1/2 of the halfThickness from the paddle center:
            backhandZone.transform.localPosition = new Vector3(0f, 0f, -halfThickness * 0.5f);

            var backhandCollider = backhandZone.AddComponent<BoxCollider>();
            backhandCollider.size = new Vector3(fullWidth, _config.Paddle.HeadThicknessMeters * 0.5f, fullLength);

        }
    }
}