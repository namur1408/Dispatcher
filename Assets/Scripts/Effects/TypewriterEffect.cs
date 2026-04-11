using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TypewriterEffect : MonoBehaviour
{
    public string fullText;
    public float typingSpeed = 0.05f;

    private TMP_Text textComponent;
    private string currentText = "";

    void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        StartTyping();
    }

    public void StartTyping()
    {
        StopAllCoroutines();
        textComponent.text = " ";
        currentText = " ";
        StartCoroutine(TypeText());
    }

    private IEnumerator TypeText()
    {
        foreach (char letter in fullText.ToCharArray())
        {
            currentText += letter;
            textComponent.text = currentText;

            yield return new WaitForSeconds(typingSpeed);
        }
    }
}