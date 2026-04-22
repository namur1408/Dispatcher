using System.Collections;
using UnityEngine;
using TMPro;

public class TerminalTypewriter : MonoBehaviour
{
    public float typingSpeed = 0.03f;
    public float deletingSpeed = 0.01f;

    private TMP_Text textComponent;
    private string targetText = "";
    private Coroutine typingCoroutine;
    public bool IsTyping { get; private set; }

    void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
        textComponent.text = "";
    }

    public void SetText(string newText)
    {
        if (targetText == newText) return;
        targetText = newText;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(AnimateText());
    }

    private IEnumerator AnimateText()
    {
        IsTyping = true;

        if (textComponent.text.Length > 0)
        {
            textComponent.maxVisibleCharacters = Mathf.Min(textComponent.maxVisibleCharacters, textComponent.textInfo.characterCount);

            while (textComponent.maxVisibleCharacters > 0)
            {
                textComponent.maxVisibleCharacters--;
                yield return new WaitForSeconds(deletingSpeed);
            }
        }

        textComponent.text = targetText;
        textComponent.maxVisibleCharacters = 0;
        textComponent.ForceMeshUpdate();

        int totalChars = textComponent.textInfo.characterCount;

        for (int i = 0; i <= totalChars; i++)
        {
            textComponent.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingSpeed);
        }

        IsTyping = false;

        if (targetText.EndsWith("..."))
        {
            string baseText = targetText.Substring(0, targetText.Length - 3);
            textComponent.maxVisibleCharacters = 99999;
            while (true)
            {
                textComponent.text = baseText + ".";
                yield return new WaitForSeconds(0.4f);
                textComponent.text = baseText + "..";
                yield return new WaitForSeconds(0.4f);
                textComponent.text = baseText + "...";
                yield return new WaitForSeconds(0.4f);
            }
        }
    }
}