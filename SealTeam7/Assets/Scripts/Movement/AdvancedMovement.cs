using Input;
using UnityEngine;

enum State
{
    Walking,
    Sprinting,
    Crouching,
    Sliding,
    Aerial,
    SprintAir,
    AirSlide,
    AirCrouch,
    WallRunning,
    Grappling,
    Debug
}

public class AdvancedMovement : MonoBehaviour
{
    [Header("Movement")]
    //private float moveSpeed;
    [SerializeField] private float friction;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float crouchSpeed;
    private bool _grounded;

    [Header("Arial")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float airMultiplier;
    [SerializeField] private float airRes;
    [SerializeField] private float boostForce;
    private Vector3 _momentum;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDist;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float playerHeight;

    [Header("Crouching")]
    [SerializeField] private float crouchYScale;
    private float _startYScale;
    

    [Header("Sliding")]
    [SerializeField] private float maxSlideTime;
    [SerializeField] private float slideForce;
    private float _slideTimer;
    private Vector3 _slideDir;

    [Header("Aiming")]
    [SerializeField] private Transform orientation;

    [Header("Slope Handling")]
    [SerializeField] private float maxSlopeAngle;
    private RaycastHit _slopeHit;
    private bool _exitingSlope;

    private float _horInput;
    private float _verInput;

    private Vector3 _moveDir;
    private Rigidbody _rb;

    private bool _readyToJump;
    private bool _doubleJumpReady;

    [Header("WallRunning")]
    [SerializeField] private LayerMask whatIsWall;
    [SerializeField] private float wallRunSpeed;
    [SerializeField] private float maxWallTime;
    [SerializeField] private float wallCheckDistance;
    [SerializeField] private float minJumpHeight;
    private RaycastHit _leftWallCheck;
    private RaycastHit _rightWallCheck;
    private RaycastHit _frontWallCheck;
    private RaycastHit _backWallCheck;

    [Header("Mantling")]
    [SerializeField] private float mantleCheckTotalAngle = 90;
    [SerializeField] private float mantleCheckCount = 3;
    [SerializeField] private Transform mantleCheckOrigin;
    [SerializeField] private float mantleCooldown;
    private bool _readyToMantle;

    private bool _readyToWallRun;

    [Header("Debugging")]
    [SerializeField] private State curState;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float wallRunTimer;
    [SerializeField] private bool leftWallHit;
    [SerializeField] private bool rightWallHit;
    [SerializeField] private bool frontWallHit;
    [SerializeField] private bool backWallHit;
    private InputController _inputController;
    
    private Animator animator;
    
    private Camera _mainCamera;



    private void Start()
    {
        _inputController = GetComponent<InputController>();
        animator = GetComponentInChildren<Animator>();  

        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;

        _readyToJump = true;
        _doubleJumpReady = true;
        _readyToWallRun = true;
        _readyToMantle = true;

        leftWallHit = false;
        rightWallHit = false;
        frontWallHit = false;
        backWallHit = false;

        _slideTimer = 0;
        wallRunTimer = 0;

        moveSpeed = walkSpeed;
        _startYScale = transform.localScale.y;

        curState = State.Walking;
        
        _mainCamera = Camera.main;
        
    }

    private void Update()
    {
        UpdateAnimatorParameters();
        UpdateOrientation();


        _grounded = Physics.CheckSphere(groundCheck.position, groundDist, whatIsGround);

        MoveInput();
        SpeedLimit();

        _rb.linearDamping = _grounded ? friction : airRes;
        
        
    }
    
    private void UpdateAnimatorParameters()
    {
        float smoothTime = 0.1f;
        float smoothHzInput = Mathf.Lerp(animator.GetFloat("hzInput"), _horInput, smoothTime);
        float smoothVInput = Mathf.Lerp(animator.GetFloat("vInput"), _verInput, smoothTime);

        animator.SetFloat("hzInput", smoothHzInput);
        animator.SetFloat("vInput", smoothVInput);

        animator.SetBool("Walking", curState == State.Walking);
        animator.SetBool("Crouching", curState == State.Crouching);
        animator.SetBool("Rifle Run", curState == State.Sprinting);
    }
    
    
    private void UpdateOrientation()
    {
        Vector3 cameraForward = Vector3.Scale(_mainCamera.transform.forward, new Vector3(1, 0, 1)).normalized;
        if (cameraForward != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }
    
    private void FixedUpdate()
    {
        if (curState == State.Sliding)
        {
            SlidingMovement();
        }
        else if (curState == State.WallRunning)
        {
            WallRunMovement();
        }
        else
        {
            MovePlayer();
        }
    }

    private void MoveInput()
    {
        //Movement Inputs
        Vector2 moveInput = _inputController.GetMoveInput();
        _horInput = moveInput.x;
        _verInput = moveInput.y;
        
        
        
        //Jumping
        if (_inputController.GetJumpInput())
        {
            if (curState == State.Crouching)
            {

                curState = State.Walking;

                transform.localScale = new Vector3(transform.localScale.x, _startYScale, transform.localScale.z);
                _rb.AddForce(Vector3.forward * 0.01f, ForceMode.Impulse);
                _rb.AddForce(Vector3.back * 0.01f, ForceMode.Impulse);

                _readyToJump = false;
                Invoke(nameof(ResetJump), jumpCooldown);
            }
            else if (curState == State.Sliding && _readyToJump)
            {
                curState = State.SprintAir;

                _readyToJump = false;
                Jump();

                transform.localScale = new Vector3(transform.localScale.x, _startYScale, transform.localScale.z);

                Invoke(nameof(ResetJump), jumpCooldown);
            }
            else if (curState == State.WallRunning)
            {
                WallJump();
            }
            else if ((curState == State.Walking || curState == State.Sprinting) && _readyToJump)
            {

                if (curState == State.Walking)
                {
                    curState = State.Aerial;
                }
                else curState = State.SprintAir;
                _readyToJump = false;
                Jump();

                Invoke(nameof(ResetJump), jumpCooldown);

            }
            else if (_doubleJumpReady)
            {

                _doubleJumpReady = false;
                BoostJump();
            }
        }

        if (_grounded)
        {
            if (curState == State.Aerial && _readyToJump)
            {
                curState = State.Walking;
            }
            else if (curState == State.SprintAir && _readyToJump)
            {
                curState = State.Sprinting;
            }
            else if (curState == State.AirCrouch && _readyToJump)
            {
                curState = State.Crouching;
            }
        }

        //Sprinting
        if (_inputController.GetSprintInput())
        {
            if (curState == State.Walking)
            {
                curState = State.Sprinting;
            }
            else if (curState == State.Crouching)
            {
                transform.localScale = new Vector3(transform.localScale.x, _startYScale, transform.localScale.z);
                _rb.AddForce(Vector3.forward * 0.01f, ForceMode.Impulse);
                _rb.AddForce(Vector3.back * 0.01f, ForceMode.Impulse);
                curState = State.Sprinting;
            }
            if (curState == State.Sliding)
            {
                SlideToSprint();
            }
        }
        if (_verInput < 0 && curState == State.Sprinting)
        {
            curState = State.Walking;
        }
        else if (_verInput == 0 && _horInput == 0 && curState == State.Sprinting)
        {
            curState = State.Walking;
        }
        else if (_verInput == 0 && _horInput == 0 && curState == State.SprintAir)
        {
            curState = State.Aerial;
        }

        //Crouching
        if (_inputController.GetCrouchInput())
        {
            if (curState == State.Walking)
            {
                // transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
                _rb.AddForce(Vector3.down * 3f, ForceMode.Impulse);
                curState = State.Crouching;
            }
            else if (curState == State.Aerial)
            {
                transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
                curState = State.AirCrouch;
            }
            else if (curState == State.SprintAir)
            {
                transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
                curState = State.AirSlide;
            }
            else if (curState == State.Crouching)
            {
                transform.localScale = new Vector3(transform.localScale.x, _startYScale, transform.localScale.z);
                _rb.AddForce(Vector3.forward * 0.01f, ForceMode.Impulse);
                _rb.AddForce(Vector3.back * 0.01f, ForceMode.Impulse);
                curState = State.Walking;
            }
            else if (curState == State.AirCrouch || curState == State.AirSlide)
            {
                transform.localScale = new Vector3(transform.localScale.x, _startYScale, transform.localScale.z);
                curState = State.Aerial;
            }
        }

        //Sliding
        if (_inputController.GetCrouchInput())
        {
            if (_verInput > 0 && curState == State.Sprinting)
            {
                StartSlide();
            }
            else if (curState == State.Sliding)
            {
                StopSlide();
            }
        }

        if (_grounded && curState == State.AirSlide)
        {
            StartSlide();
        }

        if (_grounded)
        {
            _doubleJumpReady = true;
            _readyToWallRun = true;
        }

        if (!_grounded && !OnSlope())
        {
            if (curState == State.Walking) curState = State.Aerial;
            else if (curState == State.Sprinting) curState = State.SprintAir;
            else if (curState == State.Crouching) curState = State.AirCrouch;
        }

        //Wall Running
        CheckForWall();

        //SpeedControl
        SpeedControl();
    }

    private void SpeedControl()
    {
        switch (curState)
        {
            case State.Walking:
                moveSpeed = walkSpeed;
                break;
            case State.Sprinting:
                moveSpeed = sprintSpeed;
                break;
            case State.Crouching:
                moveSpeed = crouchSpeed;
                break;
            case State.Sliding:
                moveSpeed = sprintSpeed;
                break;
            case State.Aerial:
                moveSpeed = walkSpeed;
                break;
            case State.SprintAir:
                moveSpeed = sprintSpeed;
                break;
            case State.AirCrouch:
                moveSpeed = walkSpeed;
                break;
            case State.AirSlide:
                moveSpeed = sprintSpeed;
                break;
            case State.WallRunning:
                moveSpeed = wallRunSpeed;
                break;
            default:
                moveSpeed = walkSpeed;
                break;
        }
    }

    private void SpeedLimit()
    {
        if (OnSlope() && !_exitingSlope)
        {
            //limits the sloped velocity rather than the floor plane velocity
            if (_rb.linearVelocity.magnitude > moveSpeed)
            {
                _rb.linearVelocity = _rb.linearVelocity.normalized * moveSpeed;
            }
        }
        else
        {
            Vector3 vel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            // limit velocity if needed
            if (vel.magnitude > moveSpeed)
            {
                Vector3 newVel = vel.normalized * moveSpeed;
                _rb.linearVelocity = new Vector3(newVel.x, _rb.linearVelocity.y, newVel.z);
            }
        }
    }

    private void MovePlayer()
    {
        //Calculate movement Direction
        _moveDir = orientation.forward * _verInput + orientation.right * _horInput;
        _moveDir = _moveDir.normalized;
        
        if (_grounded)
        {
            _momentum = _moveDir;
        }

        //physically move player
        if (OnSlope() && !_exitingSlope)
        {
            _rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 10.0f, ForceMode.Force);

            //stops bouncing up slopes
            if (_rb.linearVelocity.y > 0)
            {
                _rb.AddForce(Vector3.down * 20.0f, ForceMode.Force);
            }
        }
        else if (_grounded)
        {
            _rb.AddForce(_moveDir * moveSpeed * 10.0f, ForceMode.Force);
        }
        else
        {
            _rb.AddForce((_moveDir + _momentum).normalized * moveSpeed * 10.0f * airMultiplier, ForceMode.Force);
        }

        //bit jank but turn off gravity when on slope
        _rb.useGravity = !OnSlope();
    }

    private void Jump()
    {
        _exitingSlope = true;
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        _rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        _readyToJump = true;
        _exitingSlope = false;
    }

    private void BoostJump()
    {
        _exitingSlope = true;
        _readyToWallRun = true;
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        _moveDir = orientation.forward * _verInput + orientation.up + orientation.right * _horInput;
        _momentum = orientation.forward * _verInput + orientation.right * _horInput;
        _rb.AddForce(_moveDir.normalized * boostForce, ForceMode.Impulse);
    }

    private void StartSlide()
    {
        curState = State.Sliding;

        _slideDir = _moveDir.normalized;
        _slideDir.y = 0f;

        transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
        if (_grounded) _rb.AddForce(Vector3.down * 3f, ForceMode.Impulse);

        _slideTimer = maxSlideTime;
    }

    private void StopSlide()
    {
        curState = State.Crouching;
        _slideTimer = 0;

        if (OnSlope())
        {
            _rb.AddForce(Vector3.up * 0.5f, ForceMode.Impulse);
        }
    }

    private void SlideToSprint()
    {
        curState = State.Sprinting;
        _slideTimer = 0;

        transform.localScale = new Vector3(transform.localScale.x, _startYScale, transform.localScale.z);
        _rb.AddForce(Vector3.forward * 0.01f, ForceMode.Impulse);
        _rb.AddForce(Vector3.back * 0.01f, ForceMode.Impulse);

        if (OnSlope())
        {
            _rb.AddForce(Vector3.up * 0.5f, ForceMode.Impulse);
        }
    }

    private void SlidingMovement()
    {
        if (OnSlope() && !_exitingSlope)
        {
            _rb.AddForce(GetSlopeSlideDirection() * slideForce * 10.0f * (_slideTimer / maxSlideTime), ForceMode.Force);

            //stops bouncing up slopes
            if (_rb.linearVelocity.y > 0)
            {
                _slideTimer -= Time.deltaTime;
            }
        }
        else if (_grounded)
        {
            _slideTimer -= Time.deltaTime;
            _rb.AddForce(_slideDir * slideForce * 10f * (_slideTimer / maxSlideTime), ForceMode.Force);
        }
        if (_slideTimer < 0)
        {
            StopSlide();
        }
        if (!_grounded)
        {
            curState = State.AirSlide;
            _readyToJump = false;
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out _slopeHit, playerHeight * 0.5f + 0.35f))
        {
            float angle = Vector3.Angle(Vector3.up, _slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }

    private Vector3 GetSlopeSlideDirection()
    {
        return Vector3.ProjectOnPlane(_slideDir, _slopeHit.normal).normalized;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(_moveDir, _slopeHit.normal).normalized;
    }

    private void CheckForWall()
    {

        leftWallHit = Physics.Raycast(transform.position, -orientation.right, out _leftWallCheck, wallCheckDistance, whatIsWall);
        rightWallHit = Physics.Raycast(transform.position, orientation.right, out _rightWallCheck, wallCheckDistance, whatIsWall);
        backWallHit = Physics.Raycast(transform.position, -orientation.forward, out _backWallCheck, wallCheckDistance, whatIsWall);
        frontWallHit = Physics.Raycast(transform.position, orientation.forward, out _frontWallCheck, wallCheckDistance, whatIsWall);

        if (curState == State.WallRunning)
        {
            if (!leftWallHit && !rightWallHit && !backWallHit && !frontWallHit)
            {
                StopWallRun();
            }
        }
        else if (!Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround) && _readyToWallRun)
        {
            if (leftWallHit || rightWallHit || backWallHit || frontWallHit)
            {
                StartWallRun();
            }
        }
        else if ((leftWallHit || rightWallHit || backWallHit || frontWallHit) && !_grounded)
        {
            _rb.AddForce(Vector3.down * 1f, ForceMode.Force);
        }

        if (frontWallHit)
        {
            if (MantleCheck())
            {
                Mantle();
                return;
            }
        }
    }

    private void StartWallRun()
    {

        curState = State.WallRunning;

        transform.localScale = new Vector3(transform.localScale.x, _startYScale, transform.localScale.z);

        _rb.useGravity = false;
        _doubleJumpReady = true;
        _readyToWallRun = false;
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

        wallRunTimer = maxWallTime;
    }

    private void StopWallRun()
    {

        _rb.useGravity = true;
        curState = State.SprintAir;
        wallRunTimer = 0;

        Vector3 wallNormal;
        if (leftWallHit)
        {
            wallNormal = orientation.right;
        }
        else if (rightWallHit)
        {
            wallNormal = -orientation.right;
        }
        else if (frontWallHit)
        {
            wallNormal = -orientation.forward;
        }
        else if (backWallHit)
        {
            wallNormal = orientation.forward;
        }
        else
        {
            wallNormal = Vector3.forward;
        }

        _rb.AddForce(wallNormal.normalized * 3f, ForceMode.Impulse);

    }

    private void WallRunMovement()
    {

        Vector3 wallNormal;
        if (leftWallHit)
        {
            wallNormal = _leftWallCheck.normal;
            if (_verInput > 0)
            {
                Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
                _rb.AddForce(wallForward * moveSpeed * 10f, ForceMode.Force);
                wallRunTimer -= Time.deltaTime;
            }
            else if (_verInput < 0 || _horInput < 0)
            {
                wallRunTimer -= Time.deltaTime;
                _rb.linearVelocity = new Vector3(0f, 0f, 0f);
            }
            else if (_horInput > 0)
            {
                StopWallRun();
            }
            else
            {
                _rb.AddForce(Vector3.down * 5f, ForceMode.Force);
                wallRunTimer -= 2f * Time.deltaTime;
            }
        }
        else if (rightWallHit)
        {
            wallNormal = _rightWallCheck.normal;
            if (_verInput > 0)
            {
                Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
                _rb.AddForce(-wallForward * moveSpeed * 10f, ForceMode.Force);
                wallRunTimer -= Time.deltaTime;
            }
            else if (_verInput < 0 || _horInput > 0)
            {
                wallRunTimer -= Time.deltaTime;
                _rb.linearVelocity = new Vector3(0f, 0f, 0f);
            }
            else if (_horInput < 0)
            {
                StopWallRun();
            }
            else
            {
                _rb.AddForce(Vector3.down * 5f, ForceMode.Force);
                wallRunTimer -= 2f * Time.deltaTime;
            }
        }
        else if (frontWallHit)
        {
            wallNormal = _frontWallCheck.normal;
            if (_verInput > 0)
            {
                wallRunTimer -= Time.deltaTime * 1.5f;
                _rb.linearVelocity = new Vector3(0f, 0f, 0f);
                _rb.AddForce(Vector3.up * moveSpeed * 20f, ForceMode.Force);
            }
            else if (_horInput > 0)
            {
                Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
                _rb.AddForce(wallForward * moveSpeed * 10f, ForceMode.Force);
                wallRunTimer -= Time.deltaTime;
            }
            else if (_horInput < 0)
            {
                Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
                _rb.AddForce(-wallForward * moveSpeed * 10f, ForceMode.Force);
                wallRunTimer -= Time.deltaTime;
            }
            else if (_verInput < 0 && _horInput == 0)
            {
                StopWallRun();
            }
            else
            {
                _rb.AddForce(Vector3.down * 5f, ForceMode.Force);
                wallRunTimer -= 2f * Time.deltaTime;
            }
        }
        else if (backWallHit)
        {
            wallNormal = _backWallCheck.normal;
            if (_horInput < 0)
            {
                Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
                _rb.AddForce(wallForward * moveSpeed * 10f, ForceMode.Force);
                wallRunTimer -= Time.deltaTime;
            }
            else if (_horInput > 0)
            {
                Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
                _rb.AddForce(-wallForward * moveSpeed * 10f, ForceMode.Force);
                wallRunTimer -= Time.deltaTime;
            }
            else if (_verInput < 0)
            {
                wallRunTimer -= Time.deltaTime;
                _rb.linearVelocity = new Vector3(0f, 0f, 0f);
            }
            else if (_verInput > 0 && _horInput == 0)
            {
                StopWallRun();
            }
            else
            {
                _rb.AddForce(Vector3.down * 5f, ForceMode.Force);
                wallRunTimer -= 2f * Time.deltaTime;
            }
        }
        else
        {
            StopWallRun();
        }

        if (wallRunTimer <= 0)
        {
            StopWallRun();
            _readyToWallRun = false;
        }
    }

    private void ResetWallRun()
    {
        _readyToWallRun = true;
    }

    private void WallJump()
    {
        _rb.useGravity = true;
        curState = State.SprintAir;
        wallRunTimer = 0;
        Invoke(nameof(ResetWallRun), jumpCooldown * 2f);

        Vector3 wallNormal;
        if (leftWallHit)
        {
            wallNormal = orientation.right;
        }
        else if (rightWallHit)
        {
            wallNormal = -orientation.right;
        }
        else if (frontWallHit)
        {
            wallNormal = -orientation.forward;
        }
        else if (backWallHit)
        {
            wallNormal = orientation.forward;
        }
        else
        {
            wallNormal = Vector3.forward;
        }

        _momentum = wallNormal;
        _moveDir = (wallNormal.normalized + transform.up);
        _rb.AddForce(_moveDir * jumpForce, ForceMode.Impulse);
    }

    private bool MantleCheck()
    {
        bool gap = true;
        Vector3 checkDir;
        Quaternion rotate = Quaternion.Euler(0, 0, mantleCheckTotalAngle / 2);
        checkDir = rotate * orientation.forward;

        gap = gap && !(Physics.Raycast(mantleCheckOrigin.position, checkDir, wallCheckDistance, whatIsWall));

        for (int i = 1; i < mantleCheckCount + 1; i++)
        {
            rotate = Quaternion.Euler(0, 0, (mantleCheckTotalAngle / 2) - (i * mantleCheckTotalAngle / (mantleCheckCount - 1)));
            checkDir = rotate * orientation.forward;

            gap = gap && !(Physics.Raycast(mantleCheckOrigin.position, checkDir, wallCheckDistance, whatIsWall));
        }

        gap = gap && (_grounded || curState == State.WallRunning);

        return gap;
    }

    private void Mantle()
    {
        if (_readyToMantle)
        {
            _readyToMantle = false;
            _momentum = orientation.forward;

            _rb.linearVelocity = new Vector3(0, 0, 0);
            _rb.AddForce(Vector3.up * jumpForce * 2f, ForceMode.Impulse);

            if (curState == State.WallRunning)
            {
                StopWallRun();
            }

            curState = State.Aerial;

            Invoke(nameof(ResetMantle), mantleCooldown);
        }
    }

    private void ResetMantle()
    {
        _readyToMantle = true;
    }
}
