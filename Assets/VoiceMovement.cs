using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class VoiceMovement : MonoBehaviour
{
    private KeywordRecognizer keywordRecognizer;
    private Dictionary<string, Action> actions = new Dictionary<string, Action>();

    private bool moveAhead = false;  
    private bool moveBack = false;  
    private bool isGrounded = true;   

    private Rigidbody2D rb;

    public float moveSpeed = 5f;
    public float jumpForce = 10f;  
    public Transform groundCheck;  
    public float groundCheckRadius = 0.2f;  
    public LayerMask whatIsGround; 

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

       
        actions.Add("move ahead", MoveAhead);  
        actions.Add("move back", MoveBack);     
        actions.Add("jump", Jump);
        actions.Add("stop", Stop);

        keywordRecognizer = new KeywordRecognizer(actions.Keys.ToArray());
        keywordRecognizer.OnPhraseRecognized += RecognizedSpeech;
        keywordRecognizer.Start();
    }

    private void Update()
    {
        
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);

       
        if (moveAhead)
        {
            rb.linearVelocity = new Vector2(moveSpeed, rb.linearVelocity.y);  
        }
        else if (moveBack)
        {
            rb.linearVelocity = new Vector2(-moveSpeed, rb.linearVelocity.y); 
        }
    }

    private void RecognizedSpeech(PhraseRecognizedEventArgs speech)
    {
        Debug.Log(speech.text);
        actions[speech.text].Invoke();
    }

    private void MoveAhead()  
    {
        moveAhead = true;
        moveBack = false;
    }

    private void MoveBack()  
    {
        moveBack = true;
        moveAhead = false;
    }

    private void Jump()
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);  
        }
    }

    private void Stop()
    {
        moveAhead = false; 
        moveBack = false;   
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 
    }

  
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
