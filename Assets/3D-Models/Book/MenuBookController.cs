using UnityEngine;
using System.Collections;
using TMPro;

public class MenuBookController : MonoBehaviour
{
    [Header("Book Animator")]
    public Animator bookAnimator;
    public string pageIDParam = "PageID";
    
    [Header("Settings")]
    public float writingSpeed = 0.03f; 

    [Header("Individual Flip Timings")]
    [Tooltip("Delay when game starts before typing Page 1")]
    public float timeOnGameStart = 1.5f; // Set this to match your opening animation

    [Tooltip("Time for Main -> Options")]
    public float timeToOptions = 0.8f;

    [Tooltip("Time for Options -> Main")]
    public float timeFromOptions = 0.8f;

    [Tooltip("Time for Main -> Quit")]
    public float timeToQuit = 0.8f;

    [Tooltip("Time for Quit -> Main")]
    public float timeFromQuit = 0.8f;

    [Header("Pages")]
    public GameObject page1_Main;
    public GameObject page2_Options;
    public GameObject page3_Quit;

    // --- STARTUP LOGIC (FIXED) ---
    void Start()
    {
        StartCoroutine(RevealPageOnStart(page1_Main, timeOnGameStart));
    }

    private IEnumerator RevealPageOnStart(GameObject page, float delay)
    {
        // 1. Force the page active just in case
        page.SetActive(true);

        // 2. Prepare text (Hide it)
        TMP_Text[] allText = page.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in allText)
        {
            text.maxVisibleCharacters = 0; 
            text.alpha = 1; 
            text.ForceMeshUpdate(); // Force update right away
        }

        // 3. Wait for the book opening animation
        yield return new WaitForSeconds(delay);

        // 4. CRITICAL FIX: Wait one frame for safety before typing
        yield return null; 

        // 5. Type it out
        yield return StartCoroutine(TypewriteText(allText));
    }


    // --- BUTTON FUNCTIONS ---
    public void GoToOptions() 
    { 
        StartCoroutine(SwitchPage(page1_Main, page2_Options, 2, timeToOptions)); 
    }

    public void BackFromOptions() 
    { 
        StartCoroutine(SwitchPage(page2_Options, page1_Main, 1, timeFromOptions)); 
    }

    public void GoToQuitPage() 
    { 
        StartCoroutine(SwitchPage(page1_Main, page3_Quit, 3, timeToQuit)); 
    }

    public void BackFromQuit() 
    { 
        StartCoroutine(SwitchPage(page3_Quit, page1_Main, 1, timeFromQuit)); 
    }

    // --- FLIP LOGIC ---
    private IEnumerator SwitchPage(GameObject oldPage, GameObject newPage, int targetPageID, float duration)
    {
        oldPage.SetActive(false);

        TMP_Text[] allText = newPage.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in allText)
        {
            text.maxVisibleCharacters = 0; 
            text.alpha = 1; 
        }

        if(bookAnimator != null)
            bookAnimator.SetInteger(pageIDParam, targetPageID);

        yield return new WaitForSeconds(duration);

        newPage.SetActive(true);
        yield return null; 
        yield return StartCoroutine(TypewriteText(allText));
    }

    private IEnumerator TypewriteText(TMP_Text[] allText)
    {
        foreach(TMP_Text text in allText) { text.ForceMeshUpdate(); }

        bool isWriting = true;
        while (isWriting)
        {
            isWriting = false;
            foreach (TMP_Text text in allText)
            {
                if (text.maxVisibleCharacters < text.textInfo.characterCount)
                {
                    text.maxVisibleCharacters += 2; 
                    isWriting = true; 
                }
            }
            yield return new WaitForSeconds(writingSpeed);
        }
    }
}
     