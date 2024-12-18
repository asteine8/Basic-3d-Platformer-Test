using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
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
    public BoxCollider2D playerBoxColl;
    

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
    public bool underRoof;
    public bool onRightWall;
    public bool onLeftWall;
    public float groundLength = 1;
    public float roofLength = 1;
    public float wallLength = 0.425f;
    public LayerMask GroundLayer;
    [Tooltip("How far apart to space the two colliders from the center X position")]
    public Vector3 colliderOffset;
    public Vector3 wallColliderOffset;

    [Header("Crouch Settings")]
    public Vector2 StandingColliderSize;
    public Vector2 StandingColliderOffset;
    public Vector2 CrouchingColliderSize;
    public Vector2 CrouchingColliderOffset;
    public bool crouched;

    [Header("Slide Settings")]
    public bool canSlide;
    public bool isSliding;
    public float slideDuration = 2;
    public float slideDrag = 0.2f;

    // Input Actions
    private PlayerInput PlayerControls;
    private InputAction MoveAction;
    private InputAction JumpAction;
    private InputAction RunAction;
    private InputAction CrouchAction;

    [Header("Game State")]
    private Vector3 originPos;
    private bool playerFell;

    [Header("Wall Climb")]
    public bool canLatch;

    // Called when the script is loaded before Start()
    private void Awake()
    {
        PlayerControls = GetComponent<PlayerInput>();
        playerBoxColl = GetComponent<BoxCollider2D>();

        // identify player state
        originPos = transform.position;
        playerFell = false;


        // Get initial collider size and offset
        StandingColliderSize = playerBoxColl.size;
        StandingColliderOffset = playerBoxColl.offset;

        // Assign input actions
        MoveAction = PlayerControls.actions["Move"];
        JumpAction = PlayerControls.actions["Jump"];
        RunAction = PlayerControls.actions["Run"];
        CrouchAction = PlayerControls.actions["Crouch"];


    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerRb = GetComponent<Rigidbody2D>();

        // Attach Events
        JumpAction.started += onJumpPressed;

        onGround = false;
        crouched = false;
        canSlide = false;
        isSliding = false;
    }

    private void OnDestroy()
    {
        // Detach Events
        JumpAction.started -= onJumpPressed;
    }

    // Update is called once per frame
    void Update()
    {

        // Raycast to detect ground
        onGround = Physics2D.Raycast(transform.position + colliderOffset, Vector2.down, groundLength, GroundLayer)
                        || Physics2D.Raycast(transform.position - colliderOffset, Vector2.down, groundLength, GroundLayer);

        // Raycast to detect a roof
        underRoof = Physics2D.Raycast(transform.position + colliderOffset, Vector2.up, roofLength)
                        || Physics2D.Raycast(transform.position - colliderOffset, Vector2.up, roofLength);

        // detect if either on left or right wall
        onRightWall = Physics2D.Raycast(transform.position + wallColliderOffset, Vector2.right, wallLength)
                        || Physics2D.Raycast(transform.position - wallColliderOffset, Vector2.right, wallLength);

        onLeftWall = Physics2D.Raycast(transform.position + wallColliderOffset, Vector2.left, wallLength)
                        || Physics2D.Raycast(transform.position - wallColliderOffset, Vector2.left, wallLength);



        // If Jumpbutton is pressed
        if (JumpAction.WasPressedThisFrame())
        {
            jumpTimer = Time.time + jumpDelay;
        }

        direction = MoveAction.ReadValue<Vector2>();

        // check if player fell
        if (transform.position.y < -30)
        {
            playerFell = true;
            ResetPosition();
        }
    }

    private void FixedUpdate()
    {
        if (jumpTimer > Time.time && onGround)
        {
            Jump();
        }

        MoveCharacter(direction.x);

        // handle crouch
        Crouch();

        ModifyPhysics();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Debug.Log(collision.gameObject.name);
        //if (collision.gameObject.CompareTag("Ground")) onGround = true;
    }

    // Draw Gizmos
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        // Show ground detection raycasts
        Gizmos.DrawLine(transform.position + colliderOffset, transform.position + (Vector3.down* groundLength) + colliderOffset);
        Gizmos.DrawLine(transform.position - colliderOffset, transform.position + (Vector3.down * groundLength) - colliderOffset);

        // Show Roof detection raycasts
        Gizmos.DrawLine(transform.position + colliderOffset, transform.position + (Vector3.up * roofLength) + colliderOffset);
        Gizmos.DrawLine(transform.position - colliderOffset, transform.position + (Vector3.up * roofLength) - colliderOffset);

        // Show Wall detection raycasts
        Gizmos.DrawLine(transform.position + wallColliderOffset, transform.position + (Vector3.right * wallLength) + wallColliderOffset);
        Gizmos.DrawLine(transform.position - wallColliderOffset, transform.position + (Vector3.right * wallLength) - wallColliderOffset);

        Gizmos.DrawLine(transform.position + wallColliderOffset, transform.position + (Vector3.left * wallLength) + wallColliderOffset);
        Gizmos.DrawLine(transform.position - wallColliderOffset, transform.position + (Vector3.left * wallLength) - wallColliderOffset);

        Gizmos.DrawWireCube(GetComponent<BoxCollider2D>().transform.position, GetComponent<BoxCollider2D>().bounds.size);
    }

    #region Helper Funtions

    void ResetPosition()
    {
        if (playerFell == true)
        {
            transform.position = originPos;
            playerFell = false;
        }
    }

    void Crouch()
    {
        // if crouching
        Slide();
        if (CrouchAction.ReadValue<float>() == 1 && onGround)
        {
            if (canSlide == true)
            {
                playerBoxColl.size = CrouchingColliderSize;
                playerBoxColl.offset = CrouchingColliderOffset;
                StartCoroutine(sliding());
            }
            // Modify box collider
            playerBoxColl.size = CrouchingColliderSize;
            playerBoxColl.offset = CrouchingColliderOffset;
            crouched = true;


        }
        else if ((CrouchAction.ReadValue<float>() == 0 || !onGround) && !underRoof)
        {
            // Modify box collider
            playerBoxColl.size = StandingColliderSize;
            playerBoxColl.offset = StandingColliderOffset;

            crouched = false;
        }
    }



    void Slide()
    {
        if (SpeedLevel() == runSpeed)
        {
            canSlide = true;
        } else
        {
            canSlide = false;
        }
    }
    IEnumerator sliding()
    {
        while (Mathf.Abs(playerRb.linearVelocityX) > walkSpeed)
        {
            isSliding = true;
            playerRb.linearDamping = slideDrag;
            yield return null;
        }
        isSliding = false;
        //isSliding = true;
        //playerRb.linearDamping = slideDrag;
        //yield return new WaitForSeconds(slideDuration);
        //isSliding = false;

    }

    // Do a jump!   
    void Jump()
    {
        playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0);
        playerRb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        isSliding = false;
    }

    

    void MoveCharacter(float horizontal)
    {
        if (isSliding) return;

        playerRb.AddForce(Vector2.right * horizontal * moveSpeed);

        // If moving faster than maxSpeed, set speed to maxSpeed
        if (Mathf.Abs(playerRb.linearVelocity.x) > SpeedLevel()) {
            playerRb.linearVelocity = new Vector2(Mathf.Sign(playerRb.linearVelocity.x) * SpeedLevel(), playerRb.linearVelocity.y);
        }
    }
    float SpeedLevel()
    {
        if (RunAction.ReadValue<float>() == 1 && !crouched)
        {
            return runSpeed;
        } else return walkSpeed;
    }

    void ModifyPhysics()
    {
        bool changingDirections = (direction.x > 0 && playerRb.linearVelocity.x < 0)
                                    || (direction.x < 0 && playerRb.linearVelocity.x > 0);

        // Physics when on the ground (left/right movement)
        if (onGround && !isSliding)
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
        else if (!isSliding)
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

    #region Event Functions

    // Called when JumpAction.start
    void onJumpPressed(InputAction.CallbackContext context)
    {
        Debug.Log("Jump Started");
    }

    #endregion

}
