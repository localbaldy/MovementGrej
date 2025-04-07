using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float MoveSpeed;
    public float walkSpeed;
    public float runSpeed;

    public float GroundDrag;

    public float JumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool CanJump = true;

    [Header("Gravity Settings")]
    public float normalGravity = -9.81f;
    public float fallGravityMultiplier = 2.5f;

    [Header("GroundCheck")]
    public float PlayerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.C;

    [Header("SlopeMovement")]
    public float MaxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;


    public Transform Orientation;

    float HorizontalInput;
    float VerticalInput;
    

    Vector3 moveDirection;

    Rigidbody rb;

    public MovementState state;

    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        air
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        startYScale = transform.localScale.y;
    }


    // Update is called once per frame
    void Update()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down, PlayerHeight * 0.5f + 0.3f, whatIsGround);
        MyInput();
        SpeedControl();
        StateHandler();

        if (grounded)
        {
            rb.linearDamping = GroundDrag;
        }
        else
        {
            rb.linearDamping = 0;
            
        }
    }

    private void StateHandler()
    {
        if (grounded && Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            MoveSpeed = crouchSpeed;
        }

        else if (grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            MoveSpeed = runSpeed;
        }

        else if (grounded)
        {
            state = MovementState.walking;
            MoveSpeed = walkSpeed;
        }

        else
        {
            state = MovementState.air;
        }
    }

    void FixedUpdate()
    {
        MovePlayer();

        // Apply stronger gravity when falling
        if (!grounded)
        {
            rb.AddForce(Vector3.down * Mathf.Abs(normalGravity) * fallGravityMultiplier, ForceMode.Acceleration);
        }
    }

    private void MyInput()
    {
        HorizontalInput = Input.GetAxisRaw("Horizontal");
        VerticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(jumpKey) && CanJump && grounded)
        {
            CanJump = false;

            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }

        //crouch
        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        //stop crouch
        else if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }
    }

    private void MovePlayer()
    {
        moveDirection = Orientation.forward * VerticalInput + Orientation.right * HorizontalInput;


        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection() * MoveSpeed * 20f, ForceMode.Force);

            if (rb.linearVelocity.y > 0)
            {
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            }
        }

        if (grounded)
        {
            rb.AddForce(moveDirection.normalized * MoveSpeed * 10f, ForceMode.Force);
        }
        
        else if (!grounded)
        {
            rb.AddForce(moveDirection.normalized * MoveSpeed * 10f * airMultiplier, ForceMode.Force);
        }

        rb.useGravity = !OnSlope();
    }

    private void OnDrawGizmos()
    {
        //§Gizmos.draw
    }

    private void SpeedControl()
    {

        if (OnSlope() && !exitingSlope)
        {
            if (rb.linearVelocity.magnitude > MoveSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * MoveSpeed;
            }
        }

        else
        {
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            if (flatVel.magnitude > MoveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * MoveSpeed;
                rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
            }
        }


    }

    private void Jump()
    {
        exitingSlope = true;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce(transform.up * JumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        CanJump = true;

        exitingSlope = false;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, PlayerHeight * 0.5f * 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < MaxSlopeAngle && angle != 0;
        }

        Debug.Log("no raycast hit");
        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }
}
