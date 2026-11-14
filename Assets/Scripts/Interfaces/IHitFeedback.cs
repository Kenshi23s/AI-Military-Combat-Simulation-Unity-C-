using UnityEngine;

/// <summary>Object that plays a feedback when a hit lands on it.</summary>
public interface IHitFeedback
{
    /// <summary>Plays the feedback at hitPoint for a hit along hitDir.</summary>
    void FeedbackHit(Vector3 hitPoint, Vector3 hitDir);
}
