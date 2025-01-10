using UnityEngine;
using System.IO;

namespace Domain.Config
{
    /// <summary>
    /// Represents the calibration data for the paddle, including position and rotation offsets.
    /// Combines default offsets with any custom calibration values.
    /// </summary>
    public class PaddleCalibration
    {
        // Default calibration values that are always applied first
        private static readonly Vector3 DefaultPositionOffset = new(
            0.0247048139572144f,
            0.0175818838179111f,
            -0.0448632538318634f
        );

        private static readonly Quaternion DefaultRotationOffset = new(
            0.830770254135132f,
            -0.326905339956284f,
            -0.444602936506271f,
            0.072676956653595f
        );

        public Vector3 PositionOffset { get; internal set; }
        public Quaternion RotationOffset { get; internal set; }

        /// <summary>
        /// Initializes a new instance with default calibration values.
        /// </summary>
        internal PaddleCalibration()
        {
            PositionOffset = DefaultPositionOffset;
            //PositionOffset = Vector3.zero;
            RotationOffset = DefaultRotationOffset;
            //RotationOffset = Quaternion.identity;
        }

        /// <summary>
        /// Loads and combines the calibration data from a JSON file with default values.
        /// Custom calibration values are applied on top of the default offsets.
        /// Falls back to defaults if file not found or invalid.
        /// </summary>
        /// <param name="filePath">The path to the JSON file.</param>
        /// <returns>An instance of PaddleCalibration containing the combined offsets.</returns>
        public static PaddleCalibration LoadFromFile(string filePath)
        {
            var calibration = new PaddleCalibration();

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"Calibration file not found at {filePath}, using defaults");
                return calibration;
            }

            try
            {
                string jsonContent = File.ReadAllText(filePath);
                var jsonData = JsonUtility.FromJson<PaddleCalibrationJson>(jsonContent);

                // Create custom offsets from file
                Vector3 customPositionOffset = new(
                    float.Parse(jsonData.position.x),
                    float.Parse(jsonData.position.y),
                    float.Parse(jsonData.position.z)
                );

                Quaternion customRotationOffset = new(
                    float.Parse(jsonData.rotation.x),
                    float.Parse(jsonData.rotation.y),
                    float.Parse(jsonData.rotation.z),
                    float.Parse(jsonData.rotation.w)
                );

                // Combine default and custom offsets
                calibration.PositionOffset = DefaultPositionOffset + customPositionOffset;
                calibration.RotationOffset = customRotationOffset * DefaultRotationOffset;

                return calibration;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading calibration file: {e.Message}");
                return calibration;
            }
        }

        [System.Serializable]
        private class PaddleCalibrationJson
        {
            public PositionData position;
            public RotationData rotation;

            [System.Serializable]
            public class PositionData
            {
                public string x;
                public string y;
                public string z;
            }

            [System.Serializable]
            public class RotationData
            {
                public string x;
                public string y;
                public string z;
                public string w;
            }
        }
    }
}