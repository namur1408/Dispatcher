using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class EmailData
{
    public string sender;
    public string date;
    public string subject;
    [TextArea(5, 15)]
    public string body;
}

public class AegisMailApp : MonoBehaviour
{
    public static List<EmailData> globalInbox = new List<EmailData>();

    private static bool isInitialized = false;

    public Transform emailListContent;
    public GameObject emailButtonPrefab;
    public GameObject emptyStateVisual;
    public GameObject readingContentVisual;

    public TextMeshProUGUI readingSenderText;
    public TextMeshProUGUI readingSubjectText;
    public TextMeshProUGUI readingBodyText;
    public List<EmailData> defaultInbox = new List<EmailData>();

    void Awake()
    {
        if (!isInitialized)
        {
            globalInbox.AddRange(defaultInbox);
            isInitialized = true;
        }
    }

    void OnEnable()
    {
        RefreshInbox();
        ShowEmptyState();
    }

    public static void ClearInbox()
    {
        globalInbox.Clear();
        isInitialized = false;
    }

    public static void RestoreInbox(List<EmailData> savedEmails)
    {
        globalInbox = new List<EmailData>(savedEmails);
        isInitialized = true;
        
        AegisMailApp app = FindFirstObjectByType<AegisMailApp>();
        if (app != null && app.gameObject.activeInHierarchy)
        {
            app.RefreshInbox();
        }
    }

    public void RefreshInbox()
    {
        foreach (Transform child in emailListContent)
        {
            Destroy(child.gameObject);
        }

        foreach (EmailData email in globalInbox)
        {
            GameObject btnObj = Instantiate(emailButtonPrefab, emailListContent);
            EmailButtonHelper helper = btnObj.GetComponent<EmailButtonHelper>();

            if (helper != null)
            {
                helper.senderText.text = email.sender;
                helper.subjectText.text = email.subject;
                helper.dateText.text = email.date;

                EmailData emailToOpen = email;
                helper.button.onClick.AddListener(() => OpenEmail(emailToOpen));
            }
        }
    }

    private void OpenEmail(EmailData email)
    {
        emptyStateVisual.SetActive(false);
        readingContentVisual.SetActive(true);

        readingSenderText.text = "FROM: " + email.sender;
        readingSubjectText.text = "SUBJECT: " + email.subject;
        readingBodyText.text = email.body;
    }

    public void ShowEmptyState()
    {
        emptyStateVisual.SetActive(true);
        readingContentVisual.SetActive(false);
    }

    public static void ReceiveNewEmail(EmailData newEmail)
    {
        globalInbox.Insert(0, newEmail);

        AegisMailApp app = FindFirstObjectByType<AegisMailApp>();
        if (app != null && app.gameObject.activeInHierarchy)
        {
            app.RefreshInbox();
        }
    }
}