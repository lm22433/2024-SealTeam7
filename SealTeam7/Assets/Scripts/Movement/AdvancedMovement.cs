using Input;
using UnityEngine;

enum WallStates {
    forward,
    still,
    slide,
    drop
}

enum State {
    walking,
    sprinting,
    crouching,
    sliding,
    arial,
    sprintAir,
    airSlide,
    airCrouch,
    wallRunning,
    grappling,
    debug
}

public class AdvancedMovement : MonoBehaviour
{

    [Header("Movement")]
    //private float moveSpeed;
    [SerializeField] private float friction;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float crouchSpeed;
    private bool grounded;

    [Header("Arial")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float airMultiplier;
    [SerializeField] private float airRes;
    [SerializeField] private float boostForce;
    private Vector3 momentum;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDist;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float playerHeight;

    [Header("Crouching")]
    [SerializeField] private float crouchYScale;
    private float startYScale;

    [Header("Sliding")]
    [SerializeField] private float maxSlideTime;
    [SerializeField] private float slideForce;
    private float slideTimer;
    private Vector3 slideDir;

    [Header("Aiming")]
    [SerializeField] private Transform orientation;

    [Header("Slope Handling")]
    [SerializeField] private float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    private float horInput;
    private float verInput;

    private Vector3 moveDir;
    private Rigidbody rb;

    private bool readyToJump;
    private bool doubleJumpReady;

    [Header("WallRunning")]
    [SerializeField] private LayerMask whatIsWall;
    [SerializeField] private float wallRunSpeed;
    [SerializeField] private float maxWallTime;
    [SerializeField] private float wallCheckDistance;
    [SerializeField] private float minJumpHeight;
    private RaycastHit leftWallCheck;
    private RaycastHit rightWallCheck;
    private RaycastHit frontWallCheck;
    private RaycastHit backWallCheck;

    [Header("Mantling")]
    [SerializeField] private float mantleCheckTotalAngle = 90;
    [SerializeField] private float mantleCheckCount = 3;
    [SerializeField] private Transform mantleCheckOrigin;
    [SerializeField] private float mantleCooldown;
    private bool readyToMantle;


    private bool readyToWallRun;

    [Header("Debugging")]
    [SerializeField] private State curState;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float wallRunTimer;
    [SerializeField] private bool leftWallHit;
    [SerializeField] private bool rightWallHit;
    [SerializeField] private bool frontWallHit;
    [SerializeField] private bool backWallHit;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        readyToJump = true;
        doubleJumpReady = true;
        readyToWallRun = true;
        readyToMantle = true;

        leftWallHit = false;
        rightWallHit = false;
        frontWallHit = false;
        backWallHit = false;

        slideTimer = 0;
        wallRunTimer = 0;

        moveSpeed = walkSpeed;
        startYScale = transform.localScale.y;

        curState = State.walking;
    }

    // Update is called once per frame
    private void Update()
    {
        //check if grounded
        grounded = Physics.CheckSphere(groundCheck.position, groundDist, whatIsGround);
        
        MoveInput();
        SpeedLimit();

        if(grounded) {
            rb.linearDamping = friction;
        }
        else {
            rb.linearDamping = airRes;
        }
    }

    private void FixedUpdate()
    {
        if(curState == State.sliding) {
            SlidingMovement();
        }
        else if(curState == State.wallRunning) {
            WallRunMovement();
        }
        else {
            MovePlayer();
        }
    }

    private void MoveInput()
    {
        //Movement Inputs
        Vector2 moveInput = InputController.GetInstance().GetMoveInput();
        horInput = moveInput.x;
        verInput = moveInput.y;

        //Jumping
        if(InputController.GetInstance().GetJumpInput()) {
            if(curState == State.crouching) {

                curState = State.walking;

                transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
                rb.AddForce(Vector3.forward * 0.01f, ForceMode.Impulse);
                rb.AddForce(Vector3.back * 0.01f, ForceMode.Impulse); 

                readyToJump = false;
                Invoke(nameof(ResetJump), jumpCooldown);
            }
            else if(curState == State.sliding && readyToJump) {

                curState = State.sprintAir;

                readyToJump = false;
                Jump();

                transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);

                Invoke(nameof(ResetJump), jumpCooldown);
            }
            else if(curState == State.wallRunning) {
                WallJump();
            }
            else if((curState == State.walking || curState == State.sprinting) && readyToJump) {

                if(curState == State.walking) {
                    curState = State.arial;
                }
                else curState = State.sprintAir;
                readyToJump = false;
                Jump();

                Invoke(nameof(ResetJump), jumpCooldown);

            }
            else if(doubleJumpReady) {

                doubleJumpReady = false;
                BoostJump();
            }
        }

        if(grounded) {
            if(curState == State.arial && readyToJump) {
                curState = State.walking;
            }
            else if(curState == State.sprintAir && readyToJump) {
                curState = State.sprinting;
            }
            else if(curState == State.airCrouch && readyToJump) {
                curState = State.crouching;
            }
        }

        //Sprinting
        if(InputController.GetInstance().GetSprintInput()) {
            if(curState == State.walking) {
                curState = State.sprinting;
            }
            else if(curState == State.crouching) {
                transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
                rb.AddForce(Vector3.forward * 0.01f, ForceMode.Impulse);
                rb.AddForce(Vector3.back * 0.01f, ForceMode.Impulse);
                curState = State.sprinting;
            }
            if(curState == State.sliding) {
                SlideToSprint();
            }
        }
        if (verInput < 0 && curState == State.sprinting) {
            curState = State.walking;
        }
        else if(verInput == 0 && horInput == 0 && curState == State.sprinting) {
            curState = State.walking;
        }
        else if(verInput == 0 && horInput == 0 && curState == State.sprintAir) {
            curState = State.arial;
        }

        //Crouching
        if(InputController.GetInstance().GetCrouchInput()) {
            if(curState == State.walking) {
                transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
                rb.AddForce(Vector3.down * 3f, ForceMode.Impulse);
                curState = State.crouching;
            }
            else if(curState == State.arial) {
                transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
                curState = State.airCrouch; 
            }
            else if(curState == State.sprintAir) {
                transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
                curState = State.airSlide;                 
            }
            else if(curState == State.crouching) {
                transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
                rb.AddForce(Vector3.forward * 0.01f, ForceMode.Impulse);
                rb.AddForce(Vector3.back * 0.01f, ForceMode.Impulse);
                curState = State.walking;
            }
            else if(curState == State.airCrouch || curState == State.airSlide) {
                transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
                curState = State.arial;
            }
        }

        //Sliding
        if(InputController.GetInstance().GetCrouchInput()) {
            if(verInput > 0 && curState == State.sprinting) {
                StartSlide();
            }
            else if(curState == State.sliding) {
                StopSlide();
            }
        }

        if(grounded && curState == State.airSlide) {
            StartSlide();
        }

        if(grounded) {
            doubleJumpReady = true;
            readyToWallRun = true;
        }

        if(!grounded && !OnSlope()) {
            if(curState == State.walking) curState = State.arial;
            else if(curState == State.sprinting) curState = State.sprintAir;
            else if(curState == State.crouching) curState = State.airCrouch;
        }

        //Wall Running
        CheckForWall();

        //SpeedControl
        SpeedControl();
    }

    private void SpeedControl()
    {
        switch (curState) {
            case State.walking:
                moveSpeed = walkSpeed;
                break;
            case State.sprinting:
                moveSpeed = sprintSpeed;
                break;
            case State.crouching:
                moveSpeed = crouchSpeed;
                break;
            case State.sliding:
                moveSpeed = sprintSpeed;
                break;
            case State.arial:
                moveSpeed = walkSpeed;
                break;
            case State.sprintAir:
                moveSpeed = sprintSpeed;
                break;
            case State.airCrouch:
                moveSpeed = walkSpeed;
                break;
            case State.airSlide:
                moveSpeed = sprintSpeed;
                break;
            case State.wallRunning:
                moveSpeed = wallRunSpeed;
                break;
            default:
                moveSpeed = walkSpeed;
                break;
        }
    }

    private void SpeedLimit()
    {
        if(OnSlope() && !exitingSlope) {
            //limits the sloped velocity rather than the floor plane velocity
            if(rb.linearVelocity.magnitude > moveSpeed) {
                rb.linearVelocity = rb.linearVelocity.normalized * moveSpeed;
            }
        }
        else {
            Vector3 vel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            // limit velocity if needed
            if(vel.magnitude > moveSpeed)
            {
                Vector3 newVel = vel.normalized * moveSpeed;
                rb.linearVelocity = new Vector3(newVel.x, rb.linearVelocity.y, newVel.z);
            }
        }
    }

    private void MovePlayer()
    {

        //Calculate movement Direction
        moveDir = orientation.forward * verInput + orientation.right * horInput;
        moveDir = moveDir.normalized;

        if(grounded) {
            momentum = moveDir;
        }

        //physically move player
        if(OnSlope() && !exitingSlope) {
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 10.0f, ForceMode.Force);

            //stops bouncing up slopes
            if(rb.linearVelocity.y > 0) {
                rb.AddForce(Vector3.down * 20.0f, ForceMode.Force);
            }
        }
        else if(grounded){
            rb.AddForce(moveDir * moveSpeed * 10.0f, ForceMode.Force);   
        }
        else {
            rb.AddForce((moveDir + momentum).normalized * moveSpeed * 10.0f * airMultiplier, ForceMode.Force);
        }

        //bit jank but turn off gravity when on slope
        rb.useGravity = !OnSlope();
    }

    private void Jump()
    {
        exitingSlope = true;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;
    }

    private void BoostJump()
    {
        exitingSlope = true;
        readyToWallRun = true;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        moveDir = orientation.forward*verInput + orientation.up + orientation.right * horInput;
        momentum = orientation.forward*verInput + orientation.right * horInput;
        rb.AddForce(moveDir.normalized * boostForce, ForceMode.Impulse);
    }

    private void StartSlide()
    {
        curState = State.sliding;

        slideDir = moveDir.normalized;
        slideDir.y = 0f;

        transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
        if(grounded) rb.AddForce(Vector3.down * 3f, ForceMode.Impulse);

        slideTimer = maxSlideTime;
    }

    private void StopSlide()
    {
        curState = State.crouching;
        slideTimer = 0;

        if(OnSlope()) {
            rb.AddForce(Vector3.up * 0.5f, ForceMode.Impulse);
        }
    }

    private void SlideToSprint()
    {
        curState = State.sprinting;
        slideTimer = 0;

        transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        rb.AddForce(Vector3.forward * 0.01f, ForceMode.Impulse);
        rb.AddForce(Vector3.back * 0.01f, ForceMode.Impulse);

        if(OnSlope()) {
            rb.AddForce(Vector3.up * 0.5f, ForceMode.Impulse);
        }
    }

    private void SlidingMovement()
    {
        if(OnSlope() && !exitingSlope) {            
            rb.AddForce(GetSlopeSlideDirection() * slideForce * 10.0f * (slideTimer / maxSlideTime), ForceMode.Force);

            //stops bouncing up slopes
            if(rb.linearVelocity.y > 0) {
                slideTimer -= Time.deltaTime;
                //rb.AddForce(Vector3.down * 20.0f, ForceMode.Force);
            }
        }
        else if(grounded) {
            slideTimer -= Time.deltaTime;
            rb.AddForce(slideDir * slideForce * 10f * (slideTimer / maxSlideTime), ForceMode.Force);
        }
        if(slideTimer < 0) {
            StopSlide();
        }
        if(!grounded) {
            curState = State.airSlide;
            readyToJump = false;
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private bool OnSlope()
    {
        if(Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.35f)) {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }

    private Vector3 GetSlopeSlideDirection()
    {
        return Vector3.ProjectOnPlane(slideDir, slopeHit.normal).normalized;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDir, slopeHit.normal).normalized;
    }

    private void CheckForWall()
    {

        leftWallHit = Physics.Raycast(transform.position, -orientation.right, out leftWallCheck, wallCheckDistance, whatIsWall);
        rightWallHit = Physics.Raycast(transform.position, orientation.right, out rightWallCheck, wallCheckDistance, whatIsWall);
        backWallHit = Physics.Raycast(transform.position, -orientation.forward, out backWallCheck, wallCheckDistance, whatIsWall);
        frontWallHit = Physics.Raycast(transform.position, orientation.forward, out frontWallCheck, wallCheckDistance, whatIsWall);

        if(curState == State.wallRunning) {
            if(!leftWallHit && !rightWallHit && !backWallHit && !frontWallHit) {
                StopWallRun();
            }
        }
        else if(!Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround) && readyToWallRun) {
            if(leftWallHit || rightWallHit || backWallHit || frontWallHit) {
                StartWallRun();
            }
        }
        else if((leftWallHit || rightWallHit || backWallHit || frontWallHit) && !grounded) {
            rb.AddForce(Vector3.down *1f, ForceMode.Impulse);
        }

        if(frontWallHit) {
            if(MantleCheck()) {
                Mantle();
                return;
            }
        }
    }

    private void StartWallRun()
    {

        curState = State.wallRunning;

        transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);

        rb.useGravity = false;
        doubleJumpReady = true;
        readyToWallRun = false;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        wallRunTimer = maxWallTime;
    }

    private void StopWallRun()
    {

        rb.useGravity = true;
        curState = State.sprintAir;
        wallRunTimer = 0;

        Vector3 wallNormal;
        if(leftWallHit) {
            wallNormal = orientation.right;
        }
        else if(rightWallHit){
            wallNormal = -orientation.right;
        }
        else if(frontWallHit){
            wallNormal = -orientation.forward;
        }
        else if(backWallHit){
            wallNormal = orientation.forward;
        }
        else {
            wallNormal = Vector3.forward;
        }

        rb.AddForce(wallNormal.normalized * 3f, ForceMode.Impulse);

    }

    private void WallRunMovement()
    {

        Vector3 wallNormal;
        if(leftWallHit) {
            wallNormal = leftWallCheck.normal;
            if(verInput > 0) {
                Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
                rb.AddForce(wallForward * moveSpeed * 10f, ForceMode.Force);
                wallRunTimer -= Time.deltaTime;
            }
            else if(verInput < 0 || horInput < 0) {
                wallRunTimer -= Time.deltaTime;
                rb.linearVelocity = new Vector3(0f,0f,0f);
            }
            else if(horInput > 0) {
                StopWallRun();
            }
            else {
                rb.AddForce( Vector3.down * 5f, ForceMode.Force);
                wallRunTimer -= 2f * Time.deltaTime;
            } 
        }
        else if(rightWallHit) {
            wallNormal = rightWallCheck.normal;
            if(verInput > 0) {
                Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
                rb.AddForce(-wallForward * moveSpeed * 10f, ForceMode.Force);
                wallRunTimer -= Time.deltaTime;
            }
            else if(verInput < 0 || horInput > 0) {
                wallRunTimer -= Time.deltaTime;
                rb.linearVelocity = new Vector3(0f,0f,0f);
            }
            else if(horInput < 0) {
                StopWallRun();
            }
            else {
                rb.AddForce( Vector3.down * 5f, ForceMode.Force);
                wallRunTimer -= 2f * Time.deltaTime;
            } 
        }
        else if(frontWallHit) {
            wallNormal = frontWallCheck.normal;
            if(verInput > 0) {
                wallRunTimer -= Time.deltaTime * 1.5f;
                rb.linearVelocity = new Vector3(0f,0f,0f);
                rb.AddForce(Vector3.up * moveSpeed * 20f, ForceMode.Force);
            }
            else if(horInput > 0) {
                Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
                rb.AddForce(wallForward * moveSpeed * 10f, ForceMode.Force);
                wallRunTimer -= Time.deltaTime;
            }
            else if(horInput < 0) {
                Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
                rb.AddForce(-wallForward * moveSpeed * 10f, ForceMode.Force);
                wallRunTimer -= Time.deltaTime;
            }
            else if(verInput < 0 && horInput == 0) {
                StopWallRun();
            }
            else {
                rb.AddForce( Vector3.down * 5f, ForceMode.Force);
                wallRunTimer -= 2f * Time.deltaTime;
            }
        }
        else if(backWallHit) {
            wallNormal = backWallCheck.normal;
            if(horInput < 0) {
                Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
                rb.AddForce(wallForward * moveSpeed * 10f, ForceMode.Force);
                wallRunTimer -= Time.deltaTime;
            }
            else if(horInput > 0) {
                Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
                rb.AddForce(-wallForward * moveSpeed * 10f, ForceMode.Force);
                wallRunTimer -= Time.deltaTime;
            }
            else if(verInput < 0) {
                wallRunTimer -= Time.deltaTime;
                rb.linearVelocity = new Vector3(0f,0f,0f);
            }
            else if(verInput > 0 && horInput == 0) {
                StopWallRun();
            }
            else {
                rb.AddForce( Vector3.down * 5f, ForceMode.Force);
                wallRunTimer -= 2f * Time.deltaTime;
            }
        }
        else {
            StopWallRun();
        }

        if (wallRunTimer <= 0) {
            StopWallRun();
            readyToWallRun = false;
        }
    }

    private void ResetWallRun() {
        readyToWallRun = true;
    }

    private void WallJump() {
        rb.useGravity = true;
        curState = State.sprintAir;
        wallRunTimer = 0;
        Invoke(nameof(ResetWallRun), jumpCooldown * 2f);

        Vector3 wallNormal;
        if(leftWallHit) {
            wallNormal = orientation.right;
        }
        else if(rightWallHit){
            wallNormal = -orientation.right;
        }
        else if(frontWallHit){
            wallNormal = -orientation.forward;
        }
        else if(backWallHit){
            wallNormal = orientation.forward;
        }
        else {
            wallNormal = Vector3.forward;
        }

        momentum = wallNormal;
        moveDir = (wallNormal.normalized + transform.up);
        rb.AddForce(moveDir * jumpForce, ForceMode.Impulse);
    }

    private bool MantleCheck()
    {
        bool gap = true;
        Vector3 checkDir;
        Quaternion rotate = Quaternion.Euler(0, 0, mantleCheckTotalAngle / 2);
        checkDir = rotate * orientation.forward;

        gap = gap && !(Physics.Raycast(mantleCheckOrigin.position, checkDir, wallCheckDistance, whatIsWall));

        for (int i = 0; i < mantleCheckCount; i++) {
            rotate = Quaternion.Euler(0, 0, (mantleCheckTotalAngle / 2) - (i * mantleCheckTotalAngle / mantleCheckCount));
            checkDir = rotate * orientation.forward;

            gap = gap && !(Physics.Raycast(mantleCheckOrigin.position, checkDir, wallCheckDistance, whatIsWall));
        }

        gap = gap && (grounded || curState == State.wallRunning);

        return gap;
    }

    private void Mantle()
    {
        if(readyToMantle) {
            readyToMantle = false;
            momentum = orientation.forward;

            rb.linearVelocity = new Vector3(0,0,0);
            rb.AddForce(Vector3.up * jumpForce * 2f, ForceMode.Impulse);

            if(curState == State.wallRunning) {
                StopWallRun();
            }

            curState = State.arial;

            Invoke(nameof(ResetMantle), mantleCooldown);
        }
    }

    private void ResetMantle()
    {
        readyToMantle = true;
    }
}
