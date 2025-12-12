using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarAim : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string stateName = "Sparkle";
    [SerializeField] private int index;      
    [SerializeField] private float interval = 0.22f; 
    [SerializeField] private float cycle = 0.667f;     
    [SerializeField] private bool loop = true;

    private float startTime;

    private void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        startTime = Time.time + index * interval;
        if (animator) animator.speed = 1f;
    }

    private void Update()
    {
        if (!animator) return;

        if (Time.time < startTime) return;

        float t = (Time.time - startTime) % cycle;

        if (t < Time.deltaTime)
        {
            animator.Play(stateName, 0, 0f);
            animator.Update(0f);
            if (!loop) enabled = false;
        }
    }
}
