using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Game.Spells;


public  class AddForceToUnit<T> where T : MonoBehaviour, IPushable
{
    T me;
    float pushAmount;
    float pushDuration;
    UnitScale effectiveScale;
    public AddForceToUnit(T me,float pushAmount,float pushDuration = default)
    {
        this.me = me;
        this.pushAmount = pushAmount;
        if(pushDuration != 0) this.pushDuration = pushDuration;
        if(me is IMonster || me is IPlayer)
        {
            effectiveScale = me.UnitScale switch
            {
                UnitScale.player => UnitScale.AllExceptTower,
                UnitScale.small => UnitScale.PlayerAndSmall,
                UnitScale.middle => UnitScale.PlayerSmallMiddle,
                UnitScale.large => UnitScale.AllExceptTower,
                UnitScale.tower => UnitScale.AllExceptTower,
                _ => default
            };
        }
    }

    public void CompareEachUnit(UnitBase other)
    {
        var vector = other.transform.position - me.transform.position;

        var direction = vector.normalized;
       // Debug.Log(direction);
        float effectiveRadius_me = Mathf.Sqrt(Mathf.Pow(direction.x * me.rangeX, 2) + Mathf.Pow(direction.z * me.rangeZ, 2));

        float effectiveRadius_other = Mathf.Sqrt(Mathf.Pow(direction.x * other.rangeX, 2) + Mathf.Pow(direction.z * other.rangeZ, 2));

        //敵からの半径と自分の半径をつなげたとき（お互いが範囲外ぎりぎり）の長さ
        //これ以上範囲に入っていた場合、範囲内にはいっているということになる
        float minDistance = effectiveRadius_me + effectiveRadius_other;
        var flatVector = new Vector3(vector.x, 0f, vector.z);
        var distance = flatVector.magnitude;

        if (distance < minDistance) PushEachUnit(other, distance, minDistance, direction);       
    }

     async void PushEachUnit(UnitBase other, float distance,float minDistance,Vector3 direction)
     {         
            var extraDistance = minDistance - distance;
            var push = direction * extraDistance;
            push.y = 0f;
            var otherType = other.GetType();

            Vector3 targetPos_me = Vector3.zero;
            Vector3 targetPos_other = Vector3.zero;
            if (otherType == typeof(TowerControlller))
            {
                targetPos_me = me.transform.position - push;
                me.transform.position = Vector3.MoveTowards(me.transform.position, targetPos_me, pushAmount * Time.deltaTime);
            }
            else if ((other is IPlayer || other is IMonster) && me is ISpells)
            {
                Debug.Log("呪文発動");
                //Debug.Log($" 隕石のPushは{push}");
                other.isKnockBacked_Spell = true;
                push = push * pushAmount;
                targetPos_other = other.transform.position + push;
                var tween = other.transform.DOMove(targetPos_other,pushDuration);//
                var tweenTask = tween.ToUniTask();
                var waitTime = UniTask.Delay(TimeSpan.FromSeconds(pushDuration));
                await UniTask.WhenAll(waitTime,tweenTask); // 例: 0.2秒間ノックバック中
                other.isKnockBacked_Spell = false;
            }
            else if(other is IPlayer || other is IMonster)
            {
                Debug.Log("押された！！！！！");

                var otherScaleType = other.UnitScale;
                if ((otherScaleType & effectiveScale) != 0)
                {
                    other.isKnockBacked_Monster = true;
                    //targetPos_me = me.transform.position - push / 2;
                    targetPos_other = other.transform.position + push; // / 2
                    //me.transform.position = Vector3.MoveTowards(me.transform.position, targetPos_me, pushAmount * Time.deltaTime);
                    other.transform.position = Vector3.MoveTowards(other.transform.position, targetPos_other, pushAmount * Time.deltaTime);
                    await UniTask.Delay(TimeSpan.FromSeconds(0.1f)); // 例: 0.2秒間ノックバック中
                    other.isKnockBacked_Monster = false;
                }
            }
    }
    public void KeepDistance(MoveType moveType)
    {
        Debug.Log(moveType);
        List<UnitBase> filteredList = new List<UnitBase>();
        filteredList = moveType switch
        {
            MoveType.Walk or MoveType.Fly => GetUnitInRange_Monster(),
            MoveType.Spell => GetUnitInRange_Spell(),
            _ => filteredList

        };
        if (filteredList.Count == 0) return;
        filteredList.ForEach(cmp => CompareEachUnit(cmp));
    }

    List<UnitBase> GetUnitInRange_Monster()
    {
        var sortedArray = SortExtention.GetSortedArrayByDistance_Sphere<UnitBase>(me.gameObject, me.prioritizedRange);
        if (sortedArray.Length == 0) return new List<UnitBase>();
        List<UnitBase> filteredList = new List<UnitBase>();
        var myType = me.moveType;
        var effectiveSide = myType switch
        {
            MoveType.Walk => MoveType.Walk,
            MoveType.Fly => MoveType.Fly,
            _ => default
        };

        foreach (var unit in sortedArray)
        {
            var isDead = unit.isDead;
            //var fly = unit.moveType == MoveType.Fly;
            if (isDead) continue;
            if ((effectiveSide & myType) == 0 || (effectiveScale & me.UnitScale) == 0) continue;
            filteredList.Add(unit);
        }

        return filteredList;
    }

    List<UnitBase> GetUnitInRange_Spell()
    {
        var sortedArray = SortExtention.GetSortedArrayByDistance_Sphere<UnitBase>(me.gameObject, me.prioritizedRange);
        if (sortedArray.Length == 0) return new List<UnitBase>();
        List<UnitBase> filteredList = new List<UnitBase>();
        var spellBase = me as SpellBase;
       
        if (spellBase != null)
        {
            var effectiveSide = spellBase.pushEffectUnit switch
            { 
                PushEffectUnit.OnlyPlayerUnit => Side.PlayerSide,
                PushEffectUnit.OnlyEnemyUnit => Side.EnemySide,
                PushEffectUnit.AllUnit => Side.PlayerSide | Side.EnemySide,
                _=> default
            };
            foreach (var unit in sortedArray)
            {
                var isDead = unit.isDead;
                var unitSide = unit.Side;
                var targetSide = (unitSide & effectiveSide) != 0;
                var isTower = unit.UnitType == UnitType.tower;
                if (isDead || !targetSide || isTower) continue;
                filteredList.Add(unit);
            }
        }

        return filteredList;
    }
}
