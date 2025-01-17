using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    [Header("Movement")]
    [SerializeField] private float moveSpeed;

    [SerializeField] private float friction;

    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float airMultiplier;

    [SerializeField] private  float walkSpeed;
    [SerializeField] private float sprintSpeed;

    [Header("Keybinds")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDist;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float playerHeight;
    bool grounded;

    [SerializeField] private Transform orientation;

    private float horInput;
    private float verInput;

    private Vector3 moveDir;
    private Rigidbody rb;

    private bool readyToJump;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        
        readyToJump = true;
    }

    // Update is called once per frame
    private void Update()
    {

        //check if grounded
        grounded = Physics.CheckSphere(groundCheck.position, groundDist, whatIsGround);
        
        MoveInput();
        SpeedControl();

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

        if(Input.GetKey(jumpKey) && readyToJump && grounded) {
            readyToJump = false;

            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }

        if(Input.GetKey(sprintKey) && grounded && horInput == 0f && verInput > 0) {
            moveSpeed = sprintSpeed;
        }
        else {
            moveSpeed = walkSpeed;
        }
    }

    private void SpeedControl()
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

    private void ResetJump()
    {
        readyToJump = true;
    }
}
