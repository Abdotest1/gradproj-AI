using UnityEngine;
using System.Collections;

public class BookBrain : MonoBehaviour
{
    [Header("Setup")]
    public Animator bookAnimator;  // Drag the Book Object here
    
    // Drag your Page Parents here (e.g., Page1-Main, Page2-Options...)
    public GameObject page1_Main;
    public GameObject page2_Options;
    public GameObject page3_Quit;
    public GameObject page4_Cursed;

    // We use this to track where we are
    private int currentPage = 1;

    void Start()
    {
        // Start by hiding everything, then show Page 1 after a delay
        HideAllPages();
        StartCoroutine(ShowPageDelayed(1, 2.0f)); // Wait 2s for book to open
    }

    // Call this function from your Buttons!
    // Example: For Options button, type 2. For Back button, type 1.
    public void GoToPage(int pageNumber)
    {
        if (pageNumber == currentPage) return; // Already there

        // 1. Tell Animator to flip
        bookAnimator.SetInteger("PageID", pageNumber);

        // 2. Hide old buttons immediately (so they don't float during the flip)
        HideAllPages();

        // 3. Wait for flip animation, then show new buttons
        // (1.5 seconds is roughly how long a page flip takes)
        StartCoroutine(ShowPageDelayed(pageNumber, 1.0f));

        currentPage = pageNumber;
    }

    private void HideAllPages()
    {
        if(page1_Main) page1_Main.SetActive(false);
        if(page2_Options) page2_Options.SetActive(false);
        if(page3_Quit) page3_Quit.SetActive(false);
        if(page4_Cursed) page4_Cursed.SetActive(false);
    }

    IEnumerator ShowPageDelayed(int pageNum, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (pageNum == 1 && page1_Main) page1_Main.SetActive(true);
        if (pageNum == 2 && page2_Options) page2_Options.SetActive(true);
        if (pageNum == 3 && page3_Quit) page3_Quit.SetActive(true);
        if (pageNum == 4 && page4_Cursed) page4_Cursed.SetActive(true);
    }
    
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Closed!");
    }
}