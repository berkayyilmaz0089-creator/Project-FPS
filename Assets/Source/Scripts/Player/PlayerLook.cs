using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    public enum LookMode
    {
        Mouse,
        Action
    }

    [Header("Look Settings")]
    public LookMode lookMode = LookMode.Mouse;
    [Range(0.01f, 10.0f)] public float sensitivity = 2.0f;
    public float minPitch = -85.0f;
    public float maxPitch = 85.0f;

    [Header("References")]
    public Transform playerBody;
    public Transform cameraPivot;

    private float _pitch = 0.0f;
    private GameInput _gameInput;

    private void Start()
    {
        _gameInput = new GameInput();
        _gameInput.Enable();

        if (lookMode == LookMode.Mouse)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDestroy()
    {
        _gameInput.Disable();
    }

    private void Update()
    {
        Vector2 lookDelta = Vector2.zero;

        if (lookMode == LookMode.Mouse)
        {
            if (Mouse.current != null)
            {
                lookDelta = Mouse.current.delta.ReadValue() * 0.05f;
            }
        }
        else
        {
            lookDelta = _gameInput.Player.Look.ReadValue<Vector2>() * 0.35f;
        }

        if (lookDelta.x == 0.0f && lookDelta.y == 0.0f)
        {
            return;
        }

        if (lookDelta.x != 0.0f)
        {
            float yaw = lookDelta.x * sensitivity;
            playerBody.Rotate(Vector3.up * yaw);
        }

        if (lookDelta.y != 0.0f)
        {
            _pitch -= lookDelta.y * sensitivity;

            if (_pitch > maxPitch)
            {
                _pitch = maxPitch;
            }
            else if (_pitch < minPitch)
            {
                _pitch = minPitch;
            }

            cameraPivot.localRotation = Quaternion.Euler(_pitch, 0.0f, 0.0f);
        }
    }
}