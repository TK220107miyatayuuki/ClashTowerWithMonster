
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System;
using Cysharp.Threading.Tasks;
using System.Threading;
using Game.Spells;
using System.Linq;
using Unity.VisualScripting;
using static UnityEngine.UI.CanvasScaler;
public class SummonMonsterPointer : MonoBehaviour
{
    [SerializeField] NeedEnergyDisplayer needEnergyDisplayer;
    GameObject summonPointerParticle;
    GameObject selectedCardPrefab;
    Dictionary<CardName,GameObject> cardPrefabs = new Dictionary<CardName, GameObject>();
    //最初に場所を示すプレファブのほうのISummonbableを取得する
    Dictionary<CardName, SpellBase> summonbables = new Dictionary<CardName,SpellBase>();
    // " UnitBaseを取得する
    Dictionary<CardName,UnitBase> unitBases = new Dictionary<CardName,UnitBase>();
    Card currentCard;

    LineRenderer lineRenderer;
    public UnityAction OnPointerUp;
    public Func<bool> CheckCanSetMonster;
    public UnityAction<Card> OnSummonMonster;// Func<Card,bool>
    public Func<int> GetCurrentEnergy;
    public UnityAction<Card> OnSuccessSummon;
    bool onTheField = false;

    Vector3 particlePos = Vector3.zero;
    CancellationTokenSource cts = new CancellationTokenSource();
    ISummonbable summonbable;
    private void Start()
    {
        SetUpLineRenderer();
        needEnergyDisplayer.SetText();
        Func<float,float,Material,UniTask> waitAction = async (radAdjust,maxIntencity,material) =>
        {
            var baseColor = material.GetColor("_EmissionColor");
            while (!this.IsDestroyed())
            {
                var amount = (Mathf.Cos(Time.time * radAdjust) * 0.5f + 0.5f) * maxIntencity;
                material.SetColor("_EmissionColor", baseColor * amount);
                await UniTask.Yield(cancellationToken: this.GetCancellationTokenOnDestroy());
            }
        };
        lineRenderer.LitLineRendererMaterial(waitAction);
        Debug.Log(lineRenderer);
    }
    private void Update()
    {
        if (currentCard == null) return;
        var cardEnergy = currentCard.CardData.Energy;
        var currentEnergy = (int)GetCurrentEnergy?.Invoke();
        if ((InputManager.IsClickedSummonButton() && CheckCanSetMonster.Invoke())
            || InputManager.IsClickedSummonButtonOnHandField())
        {
            if (selectedCardPrefab != null && onTheField && currentEnergy >= cardEnergy) SetCardOnField();
        }

        if (currentEnergy <= cardEnergy)
        {
            if (needEnergyDisplayer != null)
            {
                if (!needEnergyDisplayer.gameObject.activeSelf) needEnergyDisplayer.gameObject.SetActive(true);
            }
            needEnergyDisplayer.RenewEnergyText(currentEnergy, cardEnergy);
        }
    }
    void LateUpdate()
    {
        SumonPointDisplay();
        PrefabActiveChange();
    }

    void SetUpLineRenderer()
    {
        lineRenderer = GetComponentInChildren<LineRenderer>();
        lineRenderer.SetUpLineRenderer();
    }

    public void GetMonsterPrefab(List<Card> hands)
    {
        foreach (Card card in hands)
        {
            var prefab = Instantiate(card.CardData.CardPrefab);
            cardPrefabs[card.CardData.CardName] = prefab;
            if(card.CardData.CardType == CardType.Spell)
            { summonbables[card.CardData.CardName] = prefab.GetComponent<SpellBase>();}
            else if(card.CardData.CardType == CardType.Monster)
            { unitBases[card.CardData.CardName] = prefab.GetComponent<UnitBase>();}
            prefab.gameObject.SetActive(false);
        }
    }
    void SumonPointDisplay()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var hits = Physics.RaycastAll(ray);
        if(hits.Length > 0)
        {
            //Spellのレイヤーだけの時はそれはgroundにhitしてないことを意味する
            if(hits.Length == 1)
            {
                var hit = hits.FirstOrDefault().collider.gameObject;
                var hitLayer = 1 << hit.gameObject.layer;
                var hitOnlySpell = hitLayer == Layers.spellLayer;
                if (hitOnlySpell)
                {
                    onTheField = false;
                    EnactivePointerEffect();
                    EnableLineRenderer();
                    return;
                }
            }
            foreach (var hit in hits)
            {
                var hitLayer = 1 << hit.collider.gameObject.layer;
                if (Layers.groundLayer == hitLayer)
                {
                    onTheField = true;
                    Debug.Log("ヒット");
                    var targetPos = hit.point;
                    var unitTargetPos = hit.point;
                    if (selectedCardPrefab != null)
                    {
                        if (summonPointerParticle != null)
                        {
                            if (!summonPointerParticle.activeSelf) summonPointerParticle.gameObject.SetActive(true);
                        }
                        targetPos.y += 0.5f;
                        unitTargetPos.y += 0.5f;
                        if(unitBases.TryGetValue(currentCard.CardData.CardName,out var unitBase))
                        {
                            if(unitBase.UnitType == UnitType.monster && unitBase.FlyingMonsterStatus != null)
                            {
                                var flyingOffsetY = unitBase.FlyingMonsterStatus.FlyingOffsetY;
                                unitTargetPos.y += flyingOffsetY;
                            }
                        }
                        selectedCardPrefab.gameObject.transform.position = unitTargetPos;
                        particlePos = targetPos;
                        //targetPos.y += 0.5f;//場合によっては直して
                        summonPointerParticle.transform.position = targetPos;
                        var displayerPos = targetPos + needEnergyDisplayer.transform.right * 3.0f;
                        needEnergyDisplayer.gameObject.transform.position = displayerPos;
                        if (currentCard.CardData.CardType == CardType.Spell)
                        {
                            var radiusX = 0f;
                            var radiusZ = 0f;
                            if (summonbables.TryGetValue(currentCard.CardData.CardName, out var spellBase))
                            {
                                radiusX = spellBase.rangeX;
                                radiusZ = spellBase.rangeZ;
                                var offsetY = 0.5f;
                                lineRenderer.DrawRange(targetPos, radiusX, radiusZ,offsetY);
                            }
                        }
                        else EnableLineRenderer();
                    }
                    break;
                }
            }        
        }
        else
        {          
            onTheField = false;
            EnactivePointerEffect();
            EnableLineRenderer();
            EnactiveNeedEnergy();
        }
    }
    void SetCardOnField()
    {
        OnSummonMonster.Invoke(currentCard);
        //if (!OnSummonMonster.Invoke(currentCard)) return;
        EnactivePointerEffect();
        EnableLineRenderer();

        cts.Cancel();
        cts.Dispose();
        cts = new CancellationTokenSource();
        var obj = Instantiate(selectedCardPrefab, selectedCardPrefab.transform.position,selectedCardPrefab.transform.rotation);
       
        SetStartCondition(obj);
        StartCoroutine(EffectManager.Instance.magicCircleEffect.SummonEffect(particlePos,currentCard.CardData.CardType));
        OnSuccessSummon?.Invoke(currentCard);
        OnPointerUp?.Invoke();
    }
    
    //選ばれているカードが変更されたとき
    public void SetMonsterPrefab(Card selectedCard)
    {
       
        if (selectedCardPrefab != null)
        {
          var currentCardData = currentCard.CardData;
          //前回のプレファブを非表示
          if (cardPrefabs.TryGetValue(currentCardData.CardName, out GameObject previousPrefab))
          {
                if (currentCardData.CardType == CardType.Monster)
                {
                    var unit = unitBases[currentCard.CardData.CardName];
                }
               previousPrefab.gameObject.SetActive(false);
          }
        }
        var selectedCardData = selectedCard.CardData;
        if(cardPrefabs.TryGetValue(selectedCardData.CardName,out GameObject cardPrefab))
        {
              if (selectedCardData.CardType == CardType.Monster)
              {
                    var unit = unitBases[selectedCardData.CardName];
                    AlphaChange(unit);
              }
              cardPrefab.gameObject.SetActive(true);
              SetSummonPointerEffect();
              selectedCardPrefab = cardPrefab;
              currentCard = selectedCard;
              if (selectedCardData.CardType == CardType.Spell) cardPrefab.gameObject.SetActive(false);
        }
    }

    void SetSummonPointerEffect()
    {
        if (summonPointerParticle == null)
        {
            summonPointerParticle = Instantiate(EffectManager.Instance.magicCircleEffect.summonPointerParticle);
        }       
        EffectManager.Instance.magicCircleEffect.PointerSummonEffect(summonPointerParticle,cts.Token).Forget();
        summonPointerParticle.gameObject.SetActive(true);
    }
    public void UnactiveCurrentPrefab()
    {
        if (selectedCardPrefab != null)
        {
            var currentCardData = currentCard.CardData;
            if (cardPrefabs.TryGetValue(currentCardData.CardName, out GameObject currentPrefab))
            {
                currentPrefab.gameObject.SetActive(false);
                selectedCardPrefab = null;
            }
        }
        currentCard = null;
    }

    public void EnactivePointerEffect()
    {
        if(summonPointerParticle != null) summonPointerParticle.SetActive(false);
    }

    public void EnactiveNeedEnergy()
    {
        if(needEnergyDisplayer != null) needEnergyDisplayer.gameObject.SetActive(false);
    }
    public void EnableLineRenderer()
    {
        if (lineRenderer != null) lineRenderer.enabled = false;
    }

    void PrefabActiveChange()
    {
        if (selectedCardPrefab != null)
        {
            var currentCardData = currentCard.CardData;
            if (cardPrefabs.TryGetValue(currentCardData.CardName, out GameObject currentPrefab))
            {
               currentPrefab.gameObject.SetActive(onTheField);//if(currentCardData.CardType == CardType.Monster) 
            }
        }
    }

    //void DrawSpellRange(Vector3 center)
    //{
    //    if (!lineRenderer.enabled)
    //    {
    //        lineRenderer.enabled = true;
    //    }
    //    var radiusX = 0f;
    //    var radiusZ = 0f;
    //    var offsetY = 0.5f;
    //    if(summonbables.TryGetValue(currentCard.CardData.CardName,out var spellBase))
    //    {
    //        radiusX = spellBase.rangeX;
    //        radiusZ = spellBase.rangeZ;
    //    }
    //    var segument = 100;
    //    lineRenderer.positionCount = segument;
    //    lineRenderer.loop = true;
    //    for (int i = 0; i < segument; i++)
    //    {
    //        var angle = ((float)i / segument) * Mathf.PI * 2;
    //        var x = Mathf.Cos(angle) * radiusX;
    //        var z = Mathf.Sin(angle) * radiusZ;
    //        var nextPos = new Vector3(x, 0, z) + center;
    //        nextPos.y = Terrain.activeTerrain.SampleHeight(nextPos) + offsetY;
    //        lineRenderer.SetPosition(i, nextPos);
    //    }
    //}

    void AlphaChange(UnitBase unit,bool isSummoned = false)
    {
        var mesh = unit.MySkinnedMeshes[0];
        var material = mesh.material;
        var color = material.color;
        if (isSummoned) color.a = 1.0f;
        else { var translusent = 0.5f; color.a = translusent; }       
        material.color = color;
    }

    void SetStartCondition(GameObject obj)
    {
        var summondCardData = currentCard.CardData;
        if (summondCardData.CardType == CardType.Monster)
        {
           var unitBase = obj.GetComponent<UnitBase>();
           if (unitBase != null) AlphaChange(unitBase, true);
        }

        summonbable = obj.GetComponent<ISummonbable>();
        summonbable.isSummoned = true;
    }
}
