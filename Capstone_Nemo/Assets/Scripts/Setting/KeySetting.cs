using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum KeyAction
{
    Up,
    Down,
    Left,
    Right,
    Interact,
    //SpaceInteract   
}

[System.Serializable]
public class KeyBinding
{
    public KeyAction action;
    public KeyCode code;
}

public class KeySetting : MonoBehaviour
{
    public static KeySetting Instance;

    public List<KeyBinding> KeyBindings = new();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        LoadKeyBindings();
    }

    public KeyCode GetKey(KeyAction action)
    {
        return KeyBindings.FirstOrDefault(K => K.action == action)?.code ?? KeyCode.None;
    }

    public void SetKey(KeyAction action, KeyCode newKey)
    {
        var binding = KeyBindings.FirstOrDefault(K => K.action == action);
        if (binding != null)
            binding.code = newKey;
        else KeyBindings.Add(new KeyBinding { action = action, code = newKey });
    }

    public void SaveKeyBindings()
    {
        foreach (var binding in KeyBindings)
        {
            PlayerPrefs.SetString($"KeyBinding_{binding.action}", binding.code.ToString());
        }
        PlayerPrefs.Save();
    }

    public void LoadKeyBindings()
    {
        KeyBindings.Clear();

        foreach (KeyAction action in System.Enum.GetValues(typeof(KeyAction)))
        {
            string key = PlayerPrefs.GetString($"KeyBinding_{action}", "");
            if (!string.IsNullOrEmpty(key) && System.Enum.TryParse(key, out KeyCode parsedKey))
            {
                KeyBindings.Add(new KeyBinding { action = action, code = parsedKey });
            }
            else
            {
                
                switch (action)
                {
                    case KeyAction.Up: SetKey(action, KeyCode.W); break;
                    case KeyAction.Down: SetKey(action, KeyCode.S); break;
                    case KeyAction.Left: SetKey(action, KeyCode.A); break;
                    case KeyAction.Right: SetKey(action, KeyCode.D); break;
                    case KeyAction.Interact: SetKey(action, KeyCode.E); break;
                }
            }
        }

        Debug.Log("키 설정 불러오기 완료");
    }
}


