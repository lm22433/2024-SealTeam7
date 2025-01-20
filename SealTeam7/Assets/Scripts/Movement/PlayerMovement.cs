using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    [Header("Movement")]
    [SerializeField] private float moveSpeed;

    [SerializeField] private float friction;

    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float crouchSpeed;
    private bool sprinting;

    [Header("Aerial")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float airMultiplier;
    [SerializeField] private float boostForce;


    [Header("Keybinds")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftShift;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDist;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float playerHeight;
    private bool grounded;
    
    [Header("Crouching")]
    [SerializeField] private float crouchYScale;
    //[SerializeField] private float walkSpeed;
    private float startYScale;
    private bool crouched;


    [SerializeField] private Transform orientation;
    [SerializeField] private Transform camPos;

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

        moveSpeed = walkSpeed;
        startYScale = transform.localScale.y;
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
            rb.linearDamping = 0;
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MoveInput()
    {
        horInput = Input.GetAxisRaw("Horizontal");
        verInput = Input.GetAxisRaw("Vertical");

        //Jumping
        if(Input.GetKeyDown(jumpKey) && crouched && grounded) {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
            camPos.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
            rb.AddForce(Vector3.forward * 0.01f, ForceMode.Impulse);
            rb.AddForce(Vector3.back * 0.01f, ForceMode.Impulse);
            crouched = false;

            readyToJump = false;
            Invoke(nameof(ResetJump), jumpCooldown);
        }
        else if(Input.GetKeyDown(jumpKey) && readyToJump && grounded) {
            readyToJump = false;
            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }
        else if(Input.GetKeyDown(jumpKey) && !grounded && doubleJumpReady) {
            doubleJumpReady = false;
            BoostJump();
        }

        //Sprinting
        if(Input.GetKeyDown(sprintKey) && !sprinting) {
            sprinting = !sprinting;
        }
        if (verInput < 0 && grounded) {
            sprinting = false;
        }
        else if(verInput == 0 && horInput == 0) {
            sprinting = false;
        }

        //Crouching
        if(Input.GetKeyDown(crouchKey) && !crouched) {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            camPos.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            if(grounded) rb.AddForce(Vector3.down * 3f, ForceMode.Impulse);
            crouched = true;

            //temp until I add sliding
            sprinting = false;
        }
        else if(Input.GetKeyDown(crouchKey) && crouched) {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
            camPos.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
            if(grounded) {
                rb.AddForce(Vector3.forward * 0.01f, ForceMode.Impulse);
                rb.AddForce(Vector3.back * 0.01f, ForceMode.Impulse);
            }
            crouched = false;
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
        Vector3 vel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // limit velocity if needed
        if(vel.magnitude > moveSpeed)
        {
            Vector3 newVel = vel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(newVel.x, rb.linearVelocity.y, newVel.z);
        }
    }

    private void MovePlayer()
    {

        //Calculate movement Direction
        moveDir = orientation.forward * verInput + orientation.right * horInput;
        moveDir = moveDir.normalized;

        //physically move player
        if(grounded) {
            rb.AddForce(moveDir * moveSpeed * 10.0f, ForceMode.Force);
        }
        else {
            rb.AddForce(moveDir * moveSpeed * 10.0f * airMultiplier, ForceMode.Force);
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void BoostJump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 moveDir = orientation.forward*verInput + orientation.up + orientation.right * horInput;
        rb.AddForce(moveDir.normalized * boostForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
    }
}
