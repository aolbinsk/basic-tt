using UnityEngine;

namespace Infrastructure.Utilities
{
    /// <summary>
    /// Monitors and logs performance metrics like FPS and frame times to the Debug Console.
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        private float _deltaTime = 0.0f;
        private float _minFrameTime = float.MaxValue;
        private float _maxFrameTime = 0.0f;
        private int _frameCount = 0;
        private float _elapsedTime = 0.0f;
        private const float LogInterval = 1.0f; // Log every 1 second

        void Update()
        {
            // Calculate delta time
            _deltaTime = Time.unscaledDeltaTime;
            _elapsedTime += _deltaTime;
            _frameCount++;

            // Update min and max frame times
            if (_deltaTime < _minFrameTime)
            {
                _minFrameTime = _deltaTime;
            }
            if (_deltaTime > _maxFrameTime)
            {
                _maxFrameTime = _deltaTime;
            }

            if (_elapsedTime >= LogInterval)
            {
                // Calculate FPS
                float fps = _frameCount / _elapsedTime;
                float avgFrameTime = _elapsedTime / _frameCount * 1000.0f; // In milliseconds
                float minFrameTimeMs = _minFrameTime * 1000.0f;
                float maxFrameTimeMs = _maxFrameTime * 1000.0f;

                // Log performance data
                Debug.LogFormat("Performance Stats - FPS: {0:F1}, Avg Frame Time: {1:F2} ms, Min Frame Time: {2:F2} ms, Max Frame Time: {3:F2} ms",
                    fps, avgFrameTime, minFrameTimeMs, maxFrameTimeMs);

                // Reset counters
                _deltaTime = 0.0f;
                _minFrameTime = float.MaxValue;
                _maxFrameTime = 0.0f;
                _frameCount = 0;
                _elapsedTime = 0.0f;
            }
        }
    }
}