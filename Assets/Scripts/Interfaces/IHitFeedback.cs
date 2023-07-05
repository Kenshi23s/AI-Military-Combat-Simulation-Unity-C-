using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHitFeedback
{
    void FeedbackHit(Vector3 hitPoint, Vector3 hitDir);
}