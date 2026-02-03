using UnityEngine;
using System.Collections;
using TMPro;

public class QuitBookSequence : MonoBehaviour
{
    [Header("UI Objects")]
    public GameObject areYouSureUI; 
    public GameObject cursedTextUI; 

    [Header("Animation Settings")]
    public Animator bookAnimator;
    public string flipTriggerName = "FlipToCursed"; 
    public string closeTriggerName = "CloseBook";

    [Header("Timing")]
    public float flipDuration = 1.5f; 
    public float readDuration = 3.0f; 
    public float closeDuration = 2.0f; 
    public float writingSpeed = 0.05f; 

    public void StartQuitSequence()
    {
        StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        // 1. Hide buttons
        if (areYouSureUI != null) areYouSureUI.SetActive(false);

        // 2. Trigger Flip
        if (bookAnimator != null) bookAnimator.SetTrigger(flipTriggerName);

        // 3. Wait for Flip
        yield return new WaitForSeconds(flipDuration);

        // 4. Activate Text Object
        if (cursedTextUI != null) 
        {
            cursedTextUI.SetActive(true);
            
            // Wait one frame for activation
            yield return null; 

            TMP_Text[] cursedTexts = cursedTextUI.GetComponentsInChildren<TMP_Text>();
            
            foreach (var text in cursedTexts)
            {
                // FORCE VISIBILITY: Set Alpha to 1 explicitly
                text.alpha = 1f;
                text.color = new Color(text.color.r, text.color.g, text.color.b, 1f);
                
                // Hide letters
                text.maxVisibleCharacters = 0;
                text.ForceMeshUpdate();
            }

            // *** THE NUCLEAR FIX: Wait until Unity admits there is text ***
            // This loop pauses the script until the text info is actually ready.
            bool textReady = false;
            int safetyCounter = 0;
            while (!textReady && safetyCounter < 10) 
            {
                textReady = true;
                foreach(var text in cursedTexts)
                {
                    text.ForceMeshUpdate();
                    if (text.textInfo.characterCount == 0 && text.text.Length > 0)
                    {
                        textReady = false; // Not ready yet
                    }
                }
                if (!textReady) yield return new WaitForSeconds(0.1f);
                safetyCounter++;
            }

            // 5. Type it out
            bool isWriting = true;
            while (isWriting)
            {
                isWriting = false;
                foreach (var text in cursedTexts)
                {
                    if (text.maxVisibleCharacters < text.textInfo.characterCount)
                    {
                        text.maxVisibleCharacters += 1; 
                        isWriting = true;
                    }
                }
                yield return new WaitForSeconds(writingSpeed);
            }
        }

        // 6. Read
        yield return new WaitForSeconds(readDuration);

        // 7. Hide Text & Close
        if (cursedTextUI != null) cursedTextUI.SetActive(false);
        if (bookAnimator != null) bookAnimator.SetTrigger(closeTriggerName);

        // 8. Wait & Quit
        yield return new WaitForSeconds(closeDuration);
        
        Debug.Log("Quitting..."); 
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}