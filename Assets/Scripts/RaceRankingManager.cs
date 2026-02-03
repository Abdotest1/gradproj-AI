using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class RaceRankingManager : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI lapText;
    public GameObject itemSlot;
    public RaceProgress playerProgress;

    [Header("Settings")]
    public int totalLaps = 3;

    private List<RaceProgress> allRacers = new List<RaceProgress>();
    private List<RaceProgress> sortedRacers = new List<RaceProgress>();
    private bool uiVisible = false;

    void Start()
    {
        allRacers = FindObjectsOfType<RaceProgress>().ToList();
        sortedRacers = new List<RaceProgress>(allRacers);

        if (playerProgress == null)
        {
            GameObject playerCollider = GameObject.Find("Collider");
            if (playerCollider != null)
            {
                playerProgress = playerCollider.GetComponent<RaceProgress>();
            }
        }

        // Hide UI initially
        HideUI();
    }

    void Update()
    {
        if (playerProgress == null)
            return;

        // Show UI when player starts the race
        if (!uiVisible && playerProgress.hasStarted && !playerProgress.hasFinished)
        {
            ShowUI();
        }

        // Hide UI when player finishes the race
        if (uiVisible && playerProgress.hasFinished)
        {
            HideUI();
        }

        if (uiVisible)
        {
            UpdateRankings();
            UpdateRankDisplay();
            UpdateLapDisplay();
        }
    }

    private void ShowUI()
    {
        uiVisible = true;

        if (rankText != null)
            rankText.gameObject.SetActive(true);

        if (lapText != null)
            lapText.gameObject.SetActive(true);

        if (itemSlot != null)
            itemSlot.SetActive(true);
    }

    private void HideUI()
    {
        uiVisible = false;

        if (rankText != null)
            rankText.gameObject.SetActive(false);

        if (lapText != null)
            lapText.gameObject.SetActive(false);

        /*if (itemSlot != null)
            itemSlot.SetActive(false);
        */
    }

    private void UpdateRankings()
    {
        sortedRacers = allRacers.OrderByDescending(r => r.TotalProgress).ToList();
    }

    private void UpdateRankDisplay()
    {
        if (rankText == null || playerProgress == null)
            return;

        int playerRank = GetRacerRank(playerProgress);
        int totalRacers = allRacers.Count;

        rankText.text = GetRankString(playerRank) + "/" + totalRacers;
    }

    private void UpdateLapDisplay()
    {
        if (lapText == null || playerProgress == null)
            return;

        int currentLap = Mathf.Min(playerProgress.currentLap + 1, totalLaps);
        lapText.text = "Lap " + currentLap + "/" + totalLaps;
    }

    public int GetRacerRank(RaceProgress racer)
    {
        int rank = sortedRacers.IndexOf(racer) + 1;
        return rank > 0 ? rank : allRacers.Count;
    }

    public List<RaceProgress> GetCurrentRankings()
    {
        return new List<RaceProgress>(sortedRacers);
    }

    private string GetRankString(int rank)
    {
        switch (rank)
        {
            case 1: return "1st";
            case 2: return "2nd";
            case 3: return "3rd";
            default: return rank + "th";
        }
    }

    public bool HasFinished(RaceProgress racer)
    {
        return racer.currentLap >= totalLaps;
    }
}