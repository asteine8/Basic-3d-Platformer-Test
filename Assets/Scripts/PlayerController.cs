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
    public LayerMask WallLayer;
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

    [Header("Glide Settings")]
    public bool isGliding;
    public float GlideFallVelocity = 0.25f;
    [Tooltip("Scales left/right movement by this much when gliding")]
    public float GlideMovementModifier = 0.25f;

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
    public bool isLatched;
    public float climbSpeed;
    public float wallSlideSpeed;
    public RaycastHit2D hitObject;
    public Vector3 wallDirection;
    public float maxWallSlideSpeed = 7;
    public bool tooHighOnWall;
    public bool tooLowOnWall;


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
        canLatch = false;
        tooHighOnWall = false;
        tooLowOnWall = false;
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



        DetectWall();
        // If Jumpbutton is pressed
        if (JumpAction.WasPressedThisFrame())
        {
            if (isLatched)
            {
                Jump();
                isLatched = false;
            }

            else if (canLatch)
            {
                isLatched = true;
                LatchToWall();
            } else if (!canLatch)
            {
                jumpTimer = Time.time + jumpDelay;
            }
            // Check if we are going to glide
            if (!onGround && !isLatched)
            {
                isGliding = true;
                
            }
        }

        // If jump button is released
        if (JumpAction.WasReleasedThisFrame())
        {
            isGliding = false;
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
        ModifyPhysics();
        MoveCharacter(direction);

        // handle crouch
        Crouch();

        
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

    void DetectWall()
    {
        

        // detect if either on left or right wall
        RaycastHit2D h1 = Physics2D.Raycast(transform.position + wallColliderOffset, Vector2.right, wallLength, WallLayer);
        RaycastHit2D h2 = Physics2D.Raycast(transform.position - wallColliderOffset, Vector2.right, wallLength, WallLayer);
        RaycastHit2D h3 = Physics2D.Raycast(transform.position + wallColliderOffset, Vector2.left, wallLength, WallLayer);
        RaycastHit2D h4 = Physics2D.Raycast(transform.position - wallColliderOffset, Vector2.left, wallLength, WallLayer);

        // did we hit something?
        if (h1) { hitObject = h1; wallDirection = Vector2.right; }
        else if (h2) { hitObject = h2; wallDirection = Vector2.right; }
        else if (h3) { hitObject = h3; wallDirection = Vector2.left; }
        else if (h4) { hitObject = h4; wallDirection = Vector2.left; }
        else
        {
            isLatched = false;
            canLatch = false;
            return; // We didn't hit anything
        }

        // If we hit smth, say we can latch
        canLatch = true;

        // Detect too high / too low on the right side
        if (wallDirection.Equals(Vector2.right))
        {
            tooHighOnWall = !h1;
            tooLowOnWall = !h2;
        }

        // Detect too high / too low on the left side
        else if (wallDirection.Equals(Vector2.left))
        {
            tooHighOnWall = !h3;
            tooLowOnWall = !h4;
        }

    }
    
    void LatchToWall()
    {
        DetectWall();

        playerRb.linearVelocityX = 0;
        playerRb.gravityScale = 0;

        // Calculate distance to the wall
        float distanceFromWall = Mathf.Abs(hitObject.distance - playerBoxColl.bounds.size.x/2);
        Debug.Log(distanceFromWall);

        // Snap to the wall
        transform.position = transform.position + distanceFromWall * wallDirection;
        Debug.Log(distanceFromWall * wallDirection);
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

    
 
    void MoveCharacter(Vector2 input)
    {
        if (isLatched)
        {   
            if (tooHighOnWall || tooLowOnWall)
            {
                playerRb.linearVelocityY = 0;
                playerRb.gravityScale = 0;
            }

            // climb
            if (input.y >= 0 && !tooHighOnWall)
            {
                playerRb.linearVelocityY = input.y * climbSpeed;
                
            }

            // slide
            else if (!tooLowOnWall)
            {
                playerRb.gravityScale = wallSlideSpeed;

                // Adjust speed to max wall slide speed
                if (Mathf.Abs(playerRb.linearVelocity.magnitude) > maxWallSlideSpeed)
                {
                    playerRb.linearVelocity = maxWallSlideSpeed * playerRb.linearVelocity / playerRb.linearVelocity.magnitude;
                }
            }
            
        }
        else
        {

            if (isSliding) return;

            playerRb.AddForce(Vector2.right * input.x * (isGliding ? (GlideMovementModifier * moveSpeed) : moveSpeed));

            // If moving faster than maxSpeed, set speed to maxSpeed
            if (Mathf.Abs(playerRb.linearVelocity.x) > SpeedLevel())
            {
                playerRb.linearVelocity = new Vector2(Mathf.Sign(playerRb.linearVelocity.x) * SpeedLevel(), playerRb.linearVelocity.y);
            }
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

        if (isLatched)
        {
            playerRb.linearVelocityX = 0;
            playerRb.gravityScale = 0;
            playerRb.linearDamping = 0;
            return;
        }
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
                // Gliding fall
                if (isGliding)
                {

                    playerRb.linearVelocityY = GlideFallVelocity;
                    playerRb.gravityScale = 0;
                }

                // Normal fall
                else
                {
                    playerRb.gravityScale = gravity * fallMultiplier;
                }
                
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
