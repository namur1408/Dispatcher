using UnityEngine;
using TMPro;

public class DecryptionMachine : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI shiftDisplay;
    public CipherWheel[] wheels;
    
    [Header("Settings")]
    public string encryptedWord = "FXXMBGZEHVT"; // Слово из скриншота
    public int currentShift = -14;

    void Start()
    {
        InitializeWheels();
    }

    void InitializeWheels()
    {
        // Инициализируем стартовые значения
        UpdateWheels();
    }

    public void ChangeShift(int amount)
    {
        currentShift += amount;
        UpdateWheels();
    }

    public void SetShift(int absoluteShift)
    {
        currentShift = absoluteShift;
        UpdateWheels();
    }

    public void SetEncryptedWord(string word)
    {
        encryptedWord = word;
        UpdateWheels();
    }

    private Coroutine spinCoroutine;

    void UpdateWheels()
    {
        if (shiftDisplay != null)
        {
            // Форматируем дисплей, чтобы показывало + или -
            shiftDisplay.text = currentShift > 0 ? "+" + currentShift : currentShift.ToString();
        }

        if (spinCoroutine != null) StopCoroutine(spinCoroutine);
        spinCoroutine = StartCoroutine(SpinSequentially());
    }

    System.Collections.IEnumerator SpinSequentially()
    {
        for (int i = 0; i < wheels.Length && i < encryptedWord.Length; i++)
        {
            char originalChar = encryptedWord[i];
            string alphabet = wheels[i].alphabet;
            int idx = alphabet.IndexOf(char.ToUpper(originalChar));
            
            if (idx >= 0)
            {
                // Сдвиг Цезаря
                int newIdx = (idx + currentShift) % alphabet.Length;
                if (newIdx < 0) newIdx += alphabet.Length;
                
                wheels[i].currentIndex = newIdx;
            }
            
            // Задержка перед началом вращения следующего барабана (эффект волны)
            yield return new WaitForSeconds(0.1f);
        }
    }
}
