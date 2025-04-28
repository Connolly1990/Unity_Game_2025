using UnityEngine;

public class PlayCreditsScroll : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

      
        Time.timeScale = 1f;

       
        animator.Play("Credit Animation"); 
    }
}
