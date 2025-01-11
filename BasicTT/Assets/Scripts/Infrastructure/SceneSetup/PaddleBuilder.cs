using Domain.Interfaces;
using Domain.Config;
using UnityEngine;

namespace Infrastructure.SceneSetup
{
    /// <summary>
    /// Responsible for constructing the paddle GameObject with proper physics, visuals, and configuration.
    /// Builds paddle in standard orientation: head centered at origin, handle along -X, forehand facing +Z.
    /// Uses intermediate GameObjects to maintain metric sizes while allowing scaled visuals.
    /// </summary>
    public class PaddleBuilder
    {
        private readonly PaddleConfig _paddleConfig;

        public PaddleBuilder(PaddleConfig paddleConfig)
        {
            _paddleConfig = paddleConfig;
        }

        public GameObject BuildPaddle()
        {
            var paddleRoot = new GameObject("Paddle")
            {
                layer = LayerMask.NameToLayer("Paddle")
            };

            // Build blade with separated physics and visuals
            var bladePivot = new GameObject("PaddleBlade");
            bladePivot.transform.SetParent(paddleRoot.transform, false);
            bladePivot.transform.localPosition = Vector3.zero;

            var bladeVisuals = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bladeVisuals.name = "PaddleBlade_Visuals";
            bladeVisuals.transform.SetParent(bladePivot.transform, false);
            bladeVisuals.transform.localPosition = Vector3.zero;
            bladeVisuals.transform.localScale = _paddleConfig.Geometry.BladeHalfExtents * 2f;

            var bladeRenderer = bladeVisuals.GetComponent<Renderer>();
            if (bladeRenderer != null)
            {
                var bladeMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = Color.gray
                };
                bladeRenderer.material = bladeMaterial;
            }
            
            SetupRubber("Forehand", bladePivot, _paddleConfig.Geometry.ForehandRubberCenter, 
                _paddleConfig.Geometry.ForehandRubberHalfExtents, Color.red);
            SetupRubber("Backhand", bladePivot, _paddleConfig.Geometry.BackhandRubberCenter, 
                _paddleConfig.Geometry.BackhandRubberHalfExtents, Color.black);

            // Build handle with separated physics and visuals
            var handlePivot = new GameObject("PaddleHandle");
            handlePivot.transform.SetParent(paddleRoot.transform, false);
            
            float handleOffsetZ = _paddleConfig.HeadLengthMeters * 0.5f + _paddleConfig.HandleLengthMeters * 0.5f;
            handlePivot.transform.localPosition = new Vector3(0f, 0f, -handleOffsetZ);
            handlePivot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var handleVisuals = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handleVisuals.name = "PaddleHandle_Visuals";
            handleVisuals.transform.SetParent(handlePivot.transform, false);
            handleVisuals.transform.localPosition = Vector3.zero;
            handleVisuals.transform.localScale = new Vector3(
                _paddleConfig.HandleRadiusMeters * 2f,
                _paddleConfig.HandleLengthMeters / 2, // Halved because Unity's cylinder is unit length, shape is two units high and one unit in diameter.
                _paddleConfig.HandleRadiusMeters * 2f);

            var handleRenderer = handleVisuals.GetComponent<Renderer>();
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
        
        private void SetupRubber(
            string name, 
            GameObject bladePivot, 
            Vector3 localCenter, 
            Vector3 halfExtents, 
            Color color)
        {
            var rubberPivot = new GameObject($"{name}Rubber");
            rubberPivot.transform.SetParent(bladePivot.transform, false);
            rubberPivot.transform.localPosition = localCenter;

            var rubberVisuals = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rubberVisuals.name = $"{name}Rubber_Visuals";
            rubberVisuals.transform.SetParent(rubberPivot.transform, false);
            rubberVisuals.transform.localPosition = Vector3.zero;
            rubberVisuals.transform.localScale = halfExtents * 2f;

            var renderer = rubberVisuals.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = color
                };
                renderer.material = material;
            }

            var collider = rubberPivot.AddComponent<BoxCollider>();
            collider.size = halfExtents * 2f;
        }
    }
}