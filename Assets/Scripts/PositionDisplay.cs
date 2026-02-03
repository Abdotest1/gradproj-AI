using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PositionDisplay : MonoBehaviour
{
    public CheckpointManager playerRacer;
    public TextMeshProUGUI positionText;
    public RaceManager raceManager;

    void Update()
    {
        if (playerRacer != null && positionText != null)
        {
            int pos = playerRacer.position;
            int total = raceManager.allRacers.Count;

            positionText.text = GetPositionText(pos) + " / " + total;
        }
    }

    string GetPositionText(int pos)
    {
        switch (pos)
        {
            case 1: return "1st";
            case 2: return "2nd";
            case 3: return "3rd";
            default: return pos + "th";
        }
    }
}