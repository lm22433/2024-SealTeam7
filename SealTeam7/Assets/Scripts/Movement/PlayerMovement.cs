using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    [Header("Movement")]
    private float moveSpeed;
    [SerializeField] private float friction;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float crouchSpeed;
    private bool sprinting;

    [Header("Aerial")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float airMultiplier;
    [SerializeField] private float airRes;
    [SerializeField] private float boostForce;


    [Header("Keybinds")]
    [SerializeField] private string jumpKey = "Jump";
    [SerializeField] private string sprintKey = "Sprint";
    [SerializeField] private string crouchKey = "Crouch";

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDist;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float playerHeight;
    private bool grounded;
    
    [Header("Crouching")]
    [SerializeField] private float crouchYScale;
    private float startYScale;
    private bool crouched;

    [Header("Sliding")]
    [SerializeField] private float maxSlideTime;
    [SerializeField] private float slideForce;
    private float slideTimer;
    private bool sliding;
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        
        readyToJump = true;
        doubleJumpReady = true;
        sprinting = false;
        crouched = false;
        exitingSlope = false;
        sliding = false;

        slideTimer = 0;


        moveSpeed = walkSpeed;
        startYScale = transform.localScale.y;
    }

    // Update is called once per frame
    private void Update()
    {

        //check if grounded
        grounded = Physics.CheckSphere(groundCheck.position, groundDist, whatIsGround);
        
        MoveInput();
        if(!sliding) {
            SpeedLimit();
        }

        if(grounded) {
            rb.linearDamping = friction;
        }
        else {
            rb.linearDamping = airRes;
        }
    }

    private void FixedUpdate()
    {
        if(sliding) {
            SlidingMovement();
        }
        else {
            MovePlayer();
        }
    }

    private void MoveInput()
    {
        horInput = Input.GetAxisRaw("Horizontal");
        verInput = Input.GetAxisRaw("Vertical");

        //Jumping
        if(Input.GetButtonDown(jumpKey) && crouched && grounded && !sliding) {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
            rb.AddForce(Vector3.forward * 0.01f, ForceMode.Impulse);
            rb.AddForce(Vector3.back * 0.01f, ForceMode.Impulse);
            crouched = false;

            readyToJump = false;
            Invoke(nameof(ResetJump), jumpCooldown);
        }
        else if(Input.GetButtonDown(jumpKey) && readyToJump && grounded && sliding) {
            readyToJump = false;
            Jump();

            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
            sliding = false;
            crouched = false;

            Invoke(nameof(ResetJump), jumpCooldown);
        }
        else if(Input.GetButtonDown(jumpKey) && readyToJump && grounded) {
            readyToJump = false;
            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }
        else if(Input.GetButtonDown(jumpKey) && !grounded && doubleJumpReady) {
            doubleJumpReady = false;
            BoostJump();
        }

        //Sprinting
        if(Input.GetButtonDown(sprintKey) && !sprinting && !crouched) {
            sprinting = !sprinting;
        }
        if (verInput < 0 && grounded && !sliding) {
            sprinting = false;
        }
        else if(verInput == 0 && horInput == 0 && !sliding) {
            sprinting = false;
        }

        //Crouching
        if(Input.GetButtonDown(crouchKey) && !crouched && !sprinting) {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            if(grounded) rb.AddForce(Vector3.down * 3f, ForceMode.Impulse);
            crouched = true;
            sprinting = false;
        }
        else if(Input.GetButtonDown(crouchKey) && crouched) {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
            if(grounded) {
                rb.AddForce(Vector3.forward * 0.01f, ForceMode.Impulse);
                rb.AddForce(Vector3.back * 0.01f, ForceMode.Impulse);
            }
            crouched = false;
        }

        //Sliding
        if(Input.GetButtonDown(crouchKey) && verInput > 0 && horInput == 0 && sprinting && !sliding) {
            StartSlide();
        }
        else if(Input.GetButtonDown(crouchKey) && sliding) {
            StopSlide();
        }

        //check if grounded
        grounded = Physics.CheckSphere(groundCheck.position, groundDist, whatIsGround);
        if(grounded) doubleJumpReady = true;

        //Controls the different speeds
        SpeedControl();
    }

    private void SpeedControl()
    {
        if(sprinting && grounded && horInput == 0f && verInput > 0) {
            moveSpeed = sprintSpeed;
        }
        else if(sprinting && grounded && verInput > 0) {
            moveSpeed = walkSpeed + (sprintSpeed - walkSpeed)*0.5f;
        }
        else if(crouched && grounded) {
            moveSpeed = crouchSpeed;
        }
        else if(!grounded) {
            moveSpeed = walkSpeed * airMultiplier;
        }
        else {
            moveSpeed = walkSpeed;
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
        else if(grounded || !sprinting){
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

        //physically move player
        if(OnSlope() && !exitingSlope) {
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 10.0f, ForceMode.Force);

            //stops bouncing up slopes
            if(rb.linearVelocity.y > 0) {
                rb.AddForce(Vector3.down * 20.0f, ForceMode.Force);
            }
        }
        else if(grounded) {
            rb.AddForce(moveDir * moveSpeed * 10.0f, ForceMode.Force);
        }
        else {
            rb.AddForce(moveDir * moveSpeed * 10.0f * airMultiplier, ForceMode.Force);
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

    private void BoostJump()
    {
        exitingSlope = true;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        moveDir = orientation.forward*verInput + orientation.up + orientation.right * horInput;
        rb.AddForce(moveDir.normalized * boostForce, ForceMode.Impulse);
        //rb.AddForce(orientation.up * boostForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;
    }

    private bool OnSlope()
    {
        if(Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.35f)) {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDir, slopeHit.normal).normalized;
    }

    private void StartSlide()
    {
        sliding = true;
        crouched = true;

        slideDir = orientation.forward.normalized;

        transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
        if(grounded) rb.AddForce(Vector3.down * 3f, ForceMode.Impulse);


        slideTimer = maxSlideTime;
        //rb.AddForce(slideDir * slideForce *5f, ForceMode.Impulse);
    }

    private Vector3 GetSlopeSlideDirection()
    {
        return Vector3.ProjectOnPlane(slideDir, slopeHit.normal).normalized;
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
            sprinting = false;
        }
        if(!grounded) {
            StopSlide();
            readyToJump = false;

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void StopSlide()
    {
        slideTimer = 0;
        sliding = false;

        if(OnSlope()) {
            rb.AddForce(Vector3.up * 0.5f, ForceMode.Impulse);
        }
    }
}
