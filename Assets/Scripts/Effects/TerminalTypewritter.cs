using System.Collections;
using UnityEngine;
using TMPro;

public class TerminalTypewriter : MonoBehaviour
{
    public float typingSpeed   = 0.03f;
    public float deletingSpeed = 0.01f;

    private TMP_Text textComponent;
    private string targetText  = "";
    private string currentText = "";
    private Coroutine typingCoroutine;

    public bool IsTyping { get; private set; }

    void Awake()
    {
        textComponent      = GetComponent<TMP_Text>();
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

        // Find the common prefix we can keep
        int commonLength = 0;
        int minLength    = Mathf.Min(currentText.Length, targetText.Length);
        for (int i = 0; i < minLength; i++)
        {
            if (currentText[i] == targetText[i]) commonLength++;
            else break;
        }

        // Delete back to the common prefix
        while (currentText.Length > commonLength)
        {
            currentText            = currentText.Substring(0, currentText.Length - 1);
            textComponent.text     = currentText;
            yield return new WaitForSeconds(deletingSpeed);
        }

        yield return new WaitForSeconds(0.05f);

        // Type the new characters, skipping rich-text tags
        for (int i = commonLength; i < targetText.Length; i++)
        {
            if (targetText[i] == '<')
            {
                int closeIndex = targetText.IndexOf('>', i);
                if (closeIndex != -1)
                {
                    currentText += targetText.Substring(i, closeIndex - i + 1);
                    i            = closeIndex;
                    continue;
                }
            }
            currentText        += targetText[i];
            textComponent.text  = currentText;
            yield return new WaitForSeconds(typingSpeed);
        }

        // FIX: The original code ran an infinite while(true) loop here for the "..."
        //      animation, which meant IsTyping was NEVER set to false.
        //      BigRadarTerminal.Start() does:
        //          yield return new WaitUntil(() => !topInfoText.IsTyping);
        //      That WaitUntil would hang forever, freezing the entire startup coroutine.
        //
        //      Fix: Set IsTyping = false FIRST, then run the dot animation with a
        //      cancellable coroutine so it stops cleanly when SetText() is called again.
        IsTyping = false;

        if (targetText.EndsWith("..."))
        {
            string baseText = targetText.Substring(0, targetText.Length - 3);
            // Run the blinking dots as a separate cancellable coroutine.
            // When SetText() is called again it will StopCoroutine(typingCoroutine)
            // which cancels this whole AnimateText coroutine tree automatically.
            yield return StartCoroutine(BlinkDots(baseText));
        }
    }

    private IEnumerator BlinkDots(string baseText)
    {
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
