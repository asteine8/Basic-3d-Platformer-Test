using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    [Header("Horizontal Movement")]
    [Tooltip("Horizontal move speed")]
    public float moveSpeed;
    

    [Header("Vertical  Movement")]
    public float jumpForce;
    [Tooltip("If the jump button is pressed this many seconds or less before on ground, jumps when on ground")]
    public float jumpDelay;
    private float jumpTimer;

    [Header("Components")]
    public Rigidbody2D playerRb;
    

    [Header("Physics")]
    private Vector2 direction;
    [Tooltip("Applied when changing directions")]
    public float linearDrag;
    [Tooltip("Base gravity multiplier for when in the air")]
    public float gravity;
    [Tooltip("Multiplies gravity when in air, halfed when rising and jump button is still pressed")]
    public float fallMultiplier;
    public float runSpeed;
    public float walkSpeed;

    [Header("Collision")]
    public bool onGround;
    public float groundLength = 1;
    public LayerMask GroundLayer;
    [Tooltip("How far apart to space the two colliders from the center X position")]
    public Vector3 colliderOffset;

    // Input Actions
    private PlayerInput PlayerControls;
    private InputAction MoveAction;
    private InputAction JumpAction;
    private InputAction RunAction;
    private InputAction CrouchAction;

    // Called when the script is loaded before Start()
    private void Awake()
    {
        PlayerControls = GetComponent<PlayerInput>();

        MoveAction = PlayerControls.actions["Move"];
        JumpAction = PlayerControls.actions["Jump"];
        RunAction = PlayerControls.actions["Run"];

        
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerRb = GetComponent<Rigidbody2D>();

        onGround = false;
    }

    // Update is called once per frame
    void Update()
    {
        

        // Raycast to detect ground
        onGround = Physics2D.Raycast(transform.position + colliderOffset, Vector2.down, groundLength, GroundLayer)
                        || Physics2D.Raycast(transform.position - colliderOffset, Vector2.down, groundLength, GroundLayer);

        

        // If Jumpbutton is pressed
        if (JumpAction.ReadValue<float>() == 1)
        {
            jumpTimer = Time.time + jumpDelay;
        }

        direction = MoveAction.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        if (jumpTimer > Time.time && onGround)
        {
            Jump();
        }

        MoveCharacter(direction.x);

        ModifyPhysics();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log(collision.gameObject.name);
        //if (collision.gameObject.CompareTag("Ground")) onGround = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position + colliderOffset, transform.position + (Vector3.down* groundLength) + colliderOffset);
        Gizmos.DrawLine(transform.position - colliderOffset, transform.position + (Vector3.down * groundLength) - colliderOffset);

    }

    #region Helper Funtions
    // Do a jump!   
    void Jump()
    {
        playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0);
        playerRb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    void MoveCharacter(float horizontal)
    {
        playerRb.AddForce(Vector2.right * horizontal * moveSpeed);

        // If moving faster than maxSpeed, set speed to maxSpeed
        if (Mathf.Abs(playerRb.linearVelocity.x) > SpeedLevel()) {
            playerRb.linearVelocity = new Vector2(Mathf.Sign(playerRb.linearVelocity.x) * SpeedLevel(), playerRb.linearVelocity.y);
        }
    }
    float SpeedLevel()
    {
        if (RunAction.ReadValue<float>() == 1)
        {
            return runSpeed;
        } else return walkSpeed;
    }

    void ModifyPhysics()
    {
        bool changingDirections = (direction.x > 0 && playerRb.linearVelocity.x < 0)
                                    || (direction.x < 0 && playerRb.linearVelocity.x > 0);

        // Physics when on the ground (left/right movement)
        if (onGround)
        {
            if (Mathf.Abs(direction.x) < 0.4f || changingDirections)
            {
                playerRb.linearDamping = linearDrag;
            } else
            {
                playerRb.linearDamping = 0f;
            }

            playerRb.gravityScale = 0;
        }

        // Physics when in the air (jumping)
        else
        {
            playerRb.gravityScale = gravity;
            playerRb.linearDamping = linearDrag * 0.15f;

            //Debug.Log("In Air");

            // Falling
            if (playerRb.linearVelocity.y <= 0)
            {
                playerRb.gravityScale = gravity * fallMultiplier;
                //Debug.Log("Falling");
            } 

            // Rising and jump button is still pressed
            else if (playerRb.linearVelocity.y > 0 && JumpAction.ReadValue<float>() != 1)
            {
                playerRb.gravityScale = gravity * fallMultiplier / 2;
            }
        }
    }

    #endregion
}
