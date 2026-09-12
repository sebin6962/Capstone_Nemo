using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Put one on an always-active scene-local HUD/Canvas object in EACH scene.
// Do not add DontDestroyOnLoad to this component's object.
public class SceneDialogueHudBinder : MonoBehaviour
{
    [Serializable]
    private class Target
    {
        public RectTransform root;
        public bool moveUp;
    }

    [SerializeField] private List<Target> targets = new();
    private NPCDialogueUIManager registeredManager;
    private Coroutine bindingCoroutine;

    private void OnEnable()
    {
        bindingCoroutine = StartCoroutine(BindWhenReady());
    }

    private IEnumerator BindWhenReady()
    {
        // Let scene Awake/Start and initial UI layout finish first.
        yield return null;
        while (NPCDialogueUIManager.Instance == null ||
               !NPCDialogueUIManager.Instance.isActiveAndEnabled)
            yield return null;

        registeredManager = NPCDialogueUIManager.Instance;
        Canvas.ForceUpdateCanvases();
        foreach (Target target in targets)
        {
            if (target != null && target.root != null)
                registeredManager.RegisterSceneDialogueHud(this, target.root, target.moveUp);
        }
        bindingCoroutine = null;
    }

    private void OnDisable()
    {
        if (bindingCoroutine != null)
            StopCoroutine(bindingCoroutine);
        bindingCoroutine = null;
        if (registeredManager != null)
            registeredManager.UnregisterSceneDialogueHud(this);
        registeredManager = null;
    }
}
