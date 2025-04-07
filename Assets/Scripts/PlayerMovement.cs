using System.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float MoveSpeed;
    public float walkSpeed;
    public float runSpeed;
    public float slideSpeed;

    private float desireMoveSpeed;
    private float LastDesiredMoveSpeed;
    private float RunWalkSpeedDiff;

    public float SpeedIncreaseMultiplier;
    public float slopeIncreaseMultiplier;

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
        sliding,
        air
    }

    public bool sliding;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        startYScale = transform.localScale.y;

        RunWalkSpeedDiff = runSpeed - walkSpeed;
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
        if (sliding)
        {
            state = MovementState.sliding;

            if (OnSlope() && rb.linearVelocity.y < 0.1f)
            {
                desireMoveSpeed = slideSpeed;
            }

            else
            {
                LastDesiredMoveSpeed = runSpeed;
            }
        }

        else if (grounded && Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            desireMoveSpeed = crouchSpeed;
        }

        else if (grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            desireMoveSpeed = runSpeed;
        }

        else if (grounded)
        {
            state = MovementState.walking;
            desireMoveSpeed = walkSpeed;
        }

        else
        {
            state = MovementState.air;
        }

        if (Mathf.Abs(desireMoveSpeed - LastDesiredMoveSpeed) > RunWalkSpeedDiff)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothlyLerpMoveSpeed());
        }
        else
        {
            MoveSpeed = desireMoveSpeed;
        }

        LastDesiredMoveSpeed = desireMoveSpeed;
    }

    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        float time = 0;
        float difference = Mathf.Abs(desireMoveSpeed - MoveSpeed);
        float startValue = MoveSpeed;

        while (time < difference)
        {
            MoveSpeed = Mathf.Lerp(startValue, desireMoveSpeed, time / difference);

            if (OnSlope())
            {
                float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f);

                time += Time.deltaTime * SpeedIncreaseMultiplier * slopeIncreaseMultiplier * slopeAngleIncrease;
            }
            else
            {
                time += Time.deltaTime * SpeedIncreaseMultiplier;
            }

            yield return null;
        }

        MoveSpeed = desireMoveSpeed;
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
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * MoveSpeed * 20f, ForceMode.Force);
            //rb.useGravity = false;

            if (rb.linearVelocity.y > 0)
            {
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            }
        }

        else if (grounded)
        {
            rb.AddForce(moveDirection.normalized * MoveSpeed * 10f, ForceMode.Force);
        }
        
        else if (!grounded)
        {
            rb.AddForce(moveDirection.normalized * MoveSpeed * 10f * airMultiplier, ForceMode.Force);
            //rb.useGravity = true;
        }

        rb.useGravity = !OnSlope();
    }

    private void OnDrawGizmos()
    {
        if (rb == null) return;

        
        Vector3 velocityDirection = rb.linearVelocity;

        
        if (velocityDirection != Vector3.zero)
        {
            Vector3 spherePos = transform.position + velocityDirection.normalized * 1.5f;

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(spherePos, 0.2f);

            
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, spherePos);
        }
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

    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, PlayerHeight * 0.5f + 0.5f, whatIsGround))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < MaxSlopeAngle && angle != 0;
        }

        return false;
    }


    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }
}
