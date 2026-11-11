using UnityEngine;
using TMPro;
using Unity.Profiling;

public class DebugText : MonoBehaviour
{
    [Header("Settings")]
    public float updateInterval = 0.25f;

    [Header("Reference")]
    public TextMeshProUGUI debugText;

    private GameInput _gameInput;
    private bool _isVisible = false;
    private float _updateTimer = 0.0f;

    private float _fps;
    private float _averageFps;
    private float _frameTimeMs;

    private int _accumulatedFrames = 0;
    private float _accumulatedTime = 0.0f;

    private ProfilerRecorder _drawCallsRecorder;
    private ProfilerRecorder _trianglesRecorder;
    private ProfilerRecorder _rigidbodiesRecorder;

    private void Start()
    {
        _gameInput = new GameInput();
        _gameInput.Enable();

        _drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        _trianglesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        _rigidbodiesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Physics, "Active Rigidbodies");

        if (debugText != null)
        {
            debugText.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_gameInput != null)
        {
            _gameInput.Disable();
            _gameInput.Dispose();
        }

        _drawCallsRecorder.Dispose();
        _trianglesRecorder.Dispose();
        _rigidbodiesRecorder.Dispose();
    }

    private void Update()
    {
        if (_gameInput.Player.Debug.WasPressedThisFrame())
        {
            _isVisible = !_isVisible;

            if (debugText != null)
            {
                debugText.gameObject.SetActive(_isVisible);
            }
        }

        if (!_isVisible)
        {
            return;
        }

        _fps = 1.0f / Time.unscaledDeltaTime;
        _frameTimeMs = Time.unscaledDeltaTime * 1000.0f;

        _accumulatedFrames++;
        _accumulatedTime += Time.unscaledDeltaTime;

        _updateTimer += Time.unscaledDeltaTime;

        if (_updateTimer >= updateInterval)
        {
            _averageFps = _accumulatedFrames / _accumulatedTime;

            UpdateText();

            _updateTimer = 0.0f;
            _accumulatedFrames = 0;
            _accumulatedTime = 0.0f;
        }
    }

    private void UpdateText()
    {
        if (debugText == null)
        {
            return;
        }

        long drawCalls = _drawCallsRecorder.Valid ? _drawCallsRecorder.LastValue : 0;
        long triangles = _trianglesRecorder.Valid ? _trianglesRecorder.LastValue : 0;
        long rigidbodies = _rigidbodiesRecorder.Valid ? _rigidbodiesRecorder.LastValue : 0;

        long totalRam = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
        long totalVram = UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver() / (1024 * 1024);

        string stats = $"FPS: {Mathf.RoundToInt(_fps)} (Avg: {Mathf.RoundToInt(_averageFps)})\n" +
                       $"Frame Time: {_frameTimeMs:F2} ms\n" +
                       $"Draw Calls: {drawCalls}\n" +
                       $"Triangles: {triangles}\n" +
                       $"Active Rigidbodies: {rigidbodies}\n" +
                       $"Total RAM: {totalRam} MB\n" +
                       $"VRAM (Est): {totalVram} MB";

        debugText.text = stats;
    }
}