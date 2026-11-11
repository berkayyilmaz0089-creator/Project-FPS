using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour, ISaveable
{
    [Header("Movement Settings")]
    public float walkSpeed = 4.0f;
    public float sprintSpeed = 7.0f;
    public float crouchSpeed = 2.0f;
    public float jumpForce = 5.0f;

    [Header("Physics Settings")]
    public float gravity = 19.62f;
    public float acceleration = 10.0f;
    public float deceleration = 12.0f;
    public float airControl = 2.0f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.15f;

    [Header("Crouch Settings")]
    public LayerMask ceilingMask;
    public float normalHeight = 1.8f;
    public float crouchHeight = 0.9f;
    public float crouchTransitionSpeed = 10.0f;

    [Header("Stamina Settings")]
    public float maxStamina = 5.0f;
    public float drainRate = 1.0f;
    public float regenRate = 1.0f;

    [Header("References")]
    public CharacterController controller;
    public Transform cameraPivot;

    public event Action OnJump;
    public event Action OnLand;

    private float _currentStamina;
    private bool _isExhausted = false;
    private bool _isCrouching = false;
    private float _originalCameraY;
    
    private float _currentControllerHeight;
    private float _currentCameraY;

    private float _coyoteTimer = 0.0f;
    private float _jumpBufferTimer = 0.0f;
    private Vector3 _velocity;
    private bool _wasMovingLastFrame = false;
    private bool _wasGrounded = true;
    private bool _skipNextCrouchRelease = false;

    private GameInput _gameInput;

    private void Start()
    {
        _gameInput = new GameInput();
        _gameInput.Enable();

        _currentStamina = maxStamina;
        _originalCameraY = cameraPivot.localPosition.y;
        
        _currentControllerHeight = normalHeight;
        _currentCameraY = _originalCameraY;

        controller.height = normalHeight;
        controller.center = new Vector3(0.0f, normalHeight * 0.5f, 0.0f);
    }

    private void OnDestroy()
    {
        _gameInput.Disable();
        _gameInput.Dispose();
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        Vector2 inputDirection = _gameInput.Player.Move.ReadValue<Vector2>();
        bool sprintHeld = _gameInput.Player.Sprint.IsPressed();
        bool crouchHeld = _gameInput.Player.Crouch.IsPressed();

        bool isGrounded = controller.isGrounded;

        if (isGrounded && !_wasGrounded)
        {
            OnLand?.Invoke();
        }
        _wasGrounded = isGrounded;

        UpdateJumpTimers(deltaTime, isGrounded);
        ApplyGravity(deltaTime, isGrounded);
        
        bool isMovingInput = inputDirection != Vector2.zero;
        bool needsMovementUpdate = isMovingInput || !isGrounded || _wasMovingLastFrame;

        if (needsMovementUpdate)
        {
            HandleMovement(inputDirection, sprintHeld, deltaTime, isGrounded);
        }
        else if (_velocity.x * _velocity.x > 0.0001f || _velocity.z * _velocity.z > 0.0001f)
        {
            _velocity.x = Mathf.MoveTowards(_velocity.x, 0.0f, deceleration * deltaTime);
            _velocity.z = Mathf.MoveTowards(_velocity.z, 0.0f, deceleration * deltaTime);
        }

        HandleJump();
        HandleCrouch(crouchHeld, deltaTime);
        HandleStamina(sprintHeld, inputDirection, deltaTime);

        if ((controller.collisionFlags & CollisionFlags.Above) != 0 && _velocity.y > 0.0f)
        {
            _velocity.y = 0.0f;
        }

        if (_velocity.sqrMagnitude > 0.001f)
        {
            controller.Move(_velocity * deltaTime);
        }

        _wasMovingLastFrame = isMovingInput;
    }

    private void ApplyGravity(float deltaTime, bool isGrounded)
    {
        if (isGrounded && _velocity.y < 0.0f)
        {
            _velocity.y = -2.0f;
            return;
        }
        
        _velocity.y -= gravity * deltaTime;
    }

    private void UpdateJumpTimers(float deltaTime, bool isGrounded)
    {
        if (isGrounded)
        {
            _coyoteTimer = coyoteTime;
        }
        else
        {
            _coyoteTimer -= deltaTime;
        }

        if (_gameInput.Player.Jump.WasPressedThisFrame())
        {
            _jumpBufferTimer = jumpBufferTime;
            return;
        }
        
        if (_jumpBufferTimer > 0.0f)
        {
            _jumpBufferTimer -= deltaTime;
        }
    }

    private void HandleMovement(Vector2 inputDirection, bool sprintHeld, float deltaTime, bool isGrounded)
    {
        Vector3 moveDirection = (transform.right * inputDirection.x + transform.forward * inputDirection.y).normalized;

        bool isMovingForward = inputDirection.y > 0.1f;
        bool canSprint = sprintHeld && !_isExhausted && !_isCrouching && isMovingForward;

        float targetSpeed = walkSpeed;

        if (_isCrouching)
        {
            targetSpeed = crouchSpeed;
        }
        else if (canSprint)
        {
            targetSpeed = sprintSpeed;
        }

        float accelRate = isGrounded ? (moveDirection != Vector3.zero ? acceleration : deceleration) : airControl;
        Vector3 targetVelocity = moveDirection * targetSpeed;

        _velocity.x = Mathf.Lerp(_velocity.x, targetVelocity.x, accelRate * deltaTime);
        _velocity.z = Mathf.Lerp(_velocity.z, targetVelocity.z, accelRate * deltaTime);
    }

    private void HandleJump()
    {
        if (_jumpBufferTimer > 0.0f && _coyoteTimer > 0.0f && !_isCrouching)
        {
            _velocity.y = jumpForce;
            _jumpBufferTimer = 0.0f;
            _coyoteTimer = 0.0f;

            OnJump?.Invoke();
        }
    }
    
    private void HandleCrouch(bool crouchHeld, float deltaTime)
    {
        if (_skipNextCrouchRelease)
        {
            _skipNextCrouchRelease = false;
            return;
        }

        if (crouchHeld)
        {
            _isCrouching = true;
        }
        else if (_isCrouching)
        {
            if (!IsCeilingBlocked())
            {
                _isCrouching = false;
            }
        }

        ApplyCrouch(deltaTime);
    }

    private bool IsCeilingBlocked()
    {
        float radius = controller.radius * 0.95f;
        Vector3 origin = transform.position + Vector3.up * (crouchHeight - radius);
        float maxDistance = normalHeight - crouchHeight + 0.1f;

        return Physics.SphereCast(origin, radius, Vector3.up, out RaycastHit hit, maxDistance, ceilingMask);
    }

    private void ApplyCrouch(float deltaTime)
    {
        float targetHeight = _isCrouching ? crouchHeight : normalHeight;
        float targetCameraY = _isCrouching ? (crouchHeight * 0.85f) : _originalCameraY;

        if (Mathf.Abs(_currentControllerHeight - targetHeight) > 0.01f)
        {
            if (!_isCrouching && IsCeilingBlocked())
            {
                _isCrouching = true;
                targetHeight = crouchHeight;
                targetCameraY = crouchHeight * 0.85f;
            }
            else
            {
                _currentControllerHeight = Mathf.Lerp(_currentControllerHeight, targetHeight, crouchTransitionSpeed * deltaTime);
                controller.height = _currentControllerHeight;
                controller.center = new Vector3(0.0f, _currentControllerHeight * 0.5f, 0.0f);
            }
        }

        if (Mathf.Abs(_currentCameraY - targetCameraY) > 0.01f)
        {
            _currentCameraY = Mathf.Lerp(_currentCameraY, targetCameraY, crouchTransitionSpeed * deltaTime);
            Vector3 camPos = cameraPivot.localPosition;
            camPos.y = _currentCameraY;
            cameraPivot.localPosition = camPos;
        }
    }

    private void ApplyCrouchInstant()
    {
        float targetHeight = _isCrouching ? crouchHeight : normalHeight;

        _currentControllerHeight = targetHeight;
        controller.height = targetHeight;
        controller.center = new Vector3(0.0f, targetHeight * 0.5f, 0.0f);

        _currentCameraY = _isCrouching ? (crouchHeight * 0.85f) : _originalCameraY;
        Vector3 camPos = cameraPivot.localPosition;
        camPos.y = _currentCameraY;
        cameraPivot.localPosition = camPos;
    }

    private void HandleStamina(bool sprintHeld, Vector2 inputDirection, float deltaTime)
    {
        bool isMovingForward = inputDirection.y > 0.1f;

        if (sprintHeld && isMovingForward && !_isCrouching && !_isExhausted)
        {
            _currentStamina -= drainRate * deltaTime;

            if (_currentStamina <= 0.0f)
            {
                _currentStamina = 0.0f;
                _isExhausted = true;
            }
            
            return;
        }
        
        if (!sprintHeld && _currentStamina < maxStamina)
        {
            _currentStamina += regenRate * deltaTime;

            if (_currentStamina >= maxStamina)
            {
                _currentStamina = maxStamina;
                _isExhausted = false;
            }
        }
    }

    public string GetSaveId()
    {
        return "PlayerMovement_ID";
    }

    public string CaptureState()
    {
        MovementSaveData data = new MovementSaveData();
        data.posX = transform.position.x;
        data.posY = transform.position.y;
        data.posZ = transform.position.z;
        data.velX = _velocity.x;
        data.velY = _velocity.y;
        data.velZ = _velocity.z;
        data.isCrouching = _isCrouching;
        data.currentStamina = _currentStamina;
        data.isExhausted = _isExhausted;
        return JsonUtility.ToJson(data);
    }

    public void RestoreState(string stateData)
    {
        MovementSaveData data = JsonUtility.FromJson<MovementSaveData>(stateData);

        controller.enabled = false;
        transform.position = new Vector3(data.posX, data.posY, data.posZ);
        controller.enabled = true;

        _velocity = new Vector3(data.velX, data.velY, data.velZ);
        _isCrouching = data.isCrouching;
        _currentStamina = data.currentStamina;
        _isExhausted = data.isExhausted;

        ApplyCrouchInstant();
        _skipNextCrouchRelease = true;
    }

    public void OnRestoreComplete()
    {
    }

    [System.Serializable]
    private class MovementSaveData
    {
        public float posX;
        public float posY;
        public float posZ;
        public float velX;
        public float velY;
        public float velZ;
        public bool isCrouching;
        public float currentStamina;
        public bool isExhausted;
    }
}