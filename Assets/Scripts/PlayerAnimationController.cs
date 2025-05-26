using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    Animator animator;
    Rigidbody2D rb;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        float move = Input.GetAxisRaw("Horizontal");
        bool isGrounded = Mathf.Abs(rb.linearVelocity.y) < 0.01f;

        animator.SetBool("isRunning", move != 0);
        animator.SetBool("isJumping", !isGrounded);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            // pular aqui também
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            animator.SetTrigger("attack");
        }

        // Exemplos:
        if (Input.GetKeyDown(KeyCode.H))
            animator.SetTrigger("hurt");

        if (Input.GetKeyDown(KeyCode.K))
            animator.SetTrigger("death");
    }
}
