using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    [Header("Horizontal Movement")]
    public float speed = 8;
    

    [Header("Vertical  Movement")]
    public float jumpForce = 8;

    [Header("Physics")]
    private Rigidbody2D playerRb;
    private Vector2 direction;

    [Header("Collision")]
    public bool onGround;

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
        float horzInput = MoveAction.ReadValue<Vector2>().x;
        playerRb.AddForce(Vector2.right * speed * horzInput);


        if (JumpAction.ReadValue<float>() == 1 && onGround)
        {
            Jump();
            onGround = false;
        }
        
    }

    // Do a jump!   
    void Jump()
    {
        playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0);
        playerRb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground")) onGround = true;
    }
    


}
