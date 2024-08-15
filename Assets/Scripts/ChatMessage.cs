using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ChatMessage : MonoBehaviour
{
    [SerializeField] private TMP_InputField textField;

    public void SetMessage(string message)
    {
        textField.text = message;
        textField.readOnly = true;
    }
}
