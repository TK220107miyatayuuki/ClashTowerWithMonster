using System;
using UnityEngine;

public interface IPushable
{
    float rangeX { get; }
    float rangeZ{get;}
    float prioritizedRange { get; }

    bool isKnockBacked_Monster { get; set; }
    bool isKnockBacked_Spell { get; set; }
    MoveType moveType { get; }
    UnitScale UnitScale { get;}
}
/// <summary>
/// Monster,またはPlayerの場合はwalkかflyかを指定、spellの場合はSpell
/// </summary>
[Flags]
public enum MoveType
{
   Walk = 1 << 0,
   Fly = 1 << 1,
   Spell = 1 << 2,
}

