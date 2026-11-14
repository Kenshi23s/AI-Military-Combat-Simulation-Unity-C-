using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

/// <summary>Registry of the CapturePoints of the match.</summary>
public class CapturePointManager : MonoSingleton<CapturePointManager>
{
    private List<CapturePoint> _capturePoints = new List<CapturePoint>();

    /// <summary>Read-only view of the registered CapturePoints.</summary>
    public ReadOnlyCollection<CapturePoint> CapturePoints { get; private set; }

    protected override void SingletonAwake()
    {
        CapturePoints = _capturePoints.AsReadOnly();
    }

    /// <summary>Registers capturePoint once.</summary>
    public void Add(CapturePoint capturePoint)
    {
        if (!_capturePoints.Contains(capturePoint))
            _capturePoints.Add(capturePoint);
    }

    /// <summary>Unregisters capturePoint.</summary>
    public void Remove(CapturePoint capturePoint)
    {
        _capturePoints.Remove(capturePoint);
    }

    /// <summary>Returns the team with the most captured points, or None on a tie.</summary>
    public MilitaryTeam WhosWinning()
    {
        int blue = 0;
        int red = 0;

        foreach (var item in _capturePoints)
        {
            switch (item.CapturedBy)
            {
                case MilitaryTeam.Blue:
                    blue++;
                    break;
                case MilitaryTeam.Red:
                    red++;
                    break;
            }
        }

        if (blue > red)
            return MilitaryTeam.Blue;

        if (red > blue)
            return MilitaryTeam.Red;

        return MilitaryTeam.None;
    }   
}
