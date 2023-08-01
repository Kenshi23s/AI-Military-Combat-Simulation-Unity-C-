using IA2;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

[RequireComponent(typeof(NewAIMovement))]
public abstract class MobileInfantry : Soldier
{

    public enum ASSAULT_INFANTRY_STATES
    {
        WAITING_ORDERS,
        MOVE_TOWARDS,
        FOLLOW_LEADER,
        DIE,
        FIRE_AT_WILL
    }

    public NewAIMovement Movement { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        Movement = GetComponent<NewAIMovement>();
    }
}
