using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] runFrames;
    [SerializeField] private Sprite jumpSprite;
    [SerializeField] private float animationFrameRate = 8f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private float horizontalInput;
    private bool jumpRequested;
    private bool isGrounded;
    private string currentAnimationState = "Idle";
    private string previousAnimationState = "";
    private float animationTimer;
    private int animationFrameIndex;

    public float Speed => Mathf.Abs(horizontalInput);
    public float HorizontalInput => horizontalInput;
    public bool IsGrounded => isGrounded;
    public float VerticalVelocity => rb != null ? rb.linearVelocity.y : 0f;
    public string CurrentAnimationState => currentAnimationState;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        isGrounded = CheckGrounded();
        horizontalInput = ReadHorizontalInput();

        if (ReadJumpDown() && isGrounded)
        {
            jumpRequested = true;
        }

        if (horizontalInput < -0.01f)
        {
            spriteRenderer.flipX = false;
        }
        else if (horizontalInput > 0.01f)
        {
            spriteRenderer.flipX = true;
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        isGrounded = CheckGrounded();

        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);

        if (jumpRequested)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpRequested = false;
            isGrounded = false;
        }
    }

    private void LateUpdate()
    {
        AnimateSprite();
    }

    private float ReadHorizontalInput()
    {
        float value = 0f;

#if ENABLE_LEGACY_INPUT_MANAGER
        value = Input.GetAxisRaw("Horizontal");
#endif

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            value = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                value -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                value += 1f;
            }
        }
#endif

        return Mathf.Clamp(value, -1f, 1f);
    }

    private bool ReadJumpDown()
    {
        bool jumpDown = false;

#if ENABLE_LEGACY_INPUT_MANAGER
        jumpDown |= Input.GetButtonDown("Jump");
#endif

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        jumpDown |= keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
#endif

        return jumpDown;
    }

    private bool CheckGrounded()
    {
        return groundCheck != null &&
            Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void UpdateAnimator()
    {
        animator.SetFloat("Speed", Mathf.Abs(horizontalInput));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);

        if (!isGrounded)
        {
            currentAnimationState = "Jump";
        }
        else if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            currentAnimationState = "Run";
        }
        else
        {
            currentAnimationState = "Idle";
        }
    }

    private void AnimateSprite()
    {
        if (currentAnimationState != previousAnimationState)
        {
            previousAnimationState = currentAnimationState;
            animationTimer = 0f;
            animationFrameIndex = 0;
        }

        Sprite[] frames = currentAnimationState == "Run" ? runFrames : idleFrames;
        if (currentAnimationState == "Jump")
        {
            if (jumpSprite != null)
            {
                spriteRenderer.sprite = jumpSprite;
            }

            return;
        }

        if (frames == null || frames.Length == 0)
        {
            return;
        }

        animationTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(1f, animationFrameRate);
        if (animationTimer >= frameDuration)
        {
            animationTimer -= frameDuration;
            animationFrameIndex = (animationFrameIndex + 1) % frames.Length;
            spriteRenderer.sprite = frames[animationFrameIndex];
        }
    }


    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
