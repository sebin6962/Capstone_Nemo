using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarAnim : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string stateName = "Sparkle";
    [SerializeField] private int index;      
    [SerializeField] private float interval = 0.22f; 
    [SerializeField] private float cycle = 0.667f;     
    [SerializeField] private bool loop = true;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float fadeInDuration = 1.2f;


    private float startTime;

    private void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        startTime = Time.time + index * interval;
        if (animator) animator.speed = 1f;

        StopAllCoroutines();
        StartCoroutine(FadeInSprite());
    }

    private IEnumerator FadeInSprite()
    {
        if (!spriteRenderer) yield break;

        var c = spriteRenderer.color;
        c.a = 0f;
        spriteRenderer.color = c;

        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / fadeInDuration);
            spriteRenderer.color = c;
            yield return null;
        }

        c.a = 1f;
        spriteRenderer.color = c;
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
