using System.Collections;
using UnityEngine;
using TMPro;

public class TypewriterEffect : MonoBehaviour
{
    public string fullText;
    public float typingSpeed = 0.05f;

    private TMP_Text textComponent;

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
        StartCoroutine(TypeText());
    }

    private IEnumerator TypeText()
    {

        textComponent.text = fullText;
        textComponent.maxVisibleCharacters = 0;
        textComponent.ForceMeshUpdate();

        int totalCharacters = textComponent.textInfo.characterCount;

        for (int i = 0; i <= totalCharacters; i++)
        {
            textComponent.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingSpeed);
        }
    }
}