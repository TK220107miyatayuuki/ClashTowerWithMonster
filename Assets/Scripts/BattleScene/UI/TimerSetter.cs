using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using System.Threading;
using DG.Tweening;
using Game.Spells;
using static UnityEditor.PlayerSettings;
using System.Drawing;


public class TimerSetter:MonoBehaviour
{
    public static TimerSetter Instance { get; private set; }

    GameObject summonTimer;
    GameObject spellTimer;

    float timerPerScale = 0.007f;
    struct TimerImages
    {
        public Image outSideImage;
        public Image inSideImage;
        public Image secondHandImage;
    }

    private async void Awake()
    {
        Instance = this;
        summonTimer = await SetFieldFromAssets.SetField<GameObject>("UI/SummonTimer");
        spellTimer = await SetFieldFromAssets.SetField<GameObject>("UI/SpellTimer");
    }

    public async UniTask StartSummonTimer(float summonTime,UnitBase targetUnit)
    {
        if(summonTimer == null) summonTimer = await SetFieldFromAssets.SetField<GameObject>("UI/SummonTimer");

        var scale = targetUnit.UnitScale;
        var meshBounds = targetUnit.MySkinnedMeshes[0].bounds.size;
        var size = Vector3.zero;

        switch (scale)
        {
            case UnitScale.small:
                size = Vector3.one * timerPerScale;
                break;
            case UnitScale.middle:
                size = Vector3.one * timerPerScale * 2f;
                break;
            case UnitScale.large:
                size = Vector3.one * timerPerScale * 2.5f;
                break;
        }

        var pos = targetUnit.transform.position + Vector3.up * meshBounds.y;
        var timerObj = Instantiate(this.summonTimer, pos, Quaternion.identity);
        timerObj.transform.localScale = size;
        var timerImages = GetTimerImages(timerObj);
        
        timerImages.outSideImage.fillAmount = 0f;
        timerImages.inSideImage.fillAmount = 0f;
        await CountTime(timerObj,timerImages,summonTime);

        timerImages.outSideImage.fillAmount = 1.0f;
        timerImages.inSideImage.fillAmount = 1.0f;

     
        var sequence = GetUIAnimations(timerObj,timerImages);
        await sequence.AsyncWaitForCompletion();
        UnityEngine.Object.Destroy(timerObj);
    }

    public async void StartSpellTimer(float spellTime,SpellBase spell)
    {
        if(spellTimer == null) spellTimer = await SetFieldFromAssets.SetField<GameObject>("UI/SpellTimer");
        var pos = spell.transform.position;
        pos.y += spell.timerOffsetY;
        var timerObj = Instantiate(this.spellTimer, pos, Quaternion.identity);
        var size = Vector3.one * timerPerScale;
        timerObj.transform.localScale = size;
        var timerImages = GetTimerImages(timerObj);

        timerImages.outSideImage.fillAmount = 0f;
        timerImages.inSideImage.fillAmount = 0f;
        await CountTime(timerObj, timerImages, spellTime);

        timerImages.outSideImage.fillAmount = 1.0f;
        timerImages.inSideImage.fillAmount = 1.0f;

        var sequence = GetUIAnimations(timerObj, timerImages);
        await sequence.AsyncWaitForCompletion();
        UnityEngine.Object.Destroy(timerObj);
    }

    Sequence GetUIAnimations(GameObject timerObj,TimerImages timerImages)
    {
        var originalScale = timerObj.transform.localScale;
        var endvalue_increase = originalScale * 3.0f;
        var endValue_down = originalScale * 0.5f;
        var duration = 0.15f;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(timerObj.transform.DOScale(endvalue_increase, duration))
            .Append(timerObj.transform.DOScale(endValue_down, duration))
            .Join(timerImages.inSideImage.DOFade(0f, duration))
            .Join(timerImages.outSideImage.DOFade(0f, duration));
        return sequence;
    }

    TimerImages GetTimerImages(GameObject timerObj)
    {
        var parent = timerObj.transform.GetChild(0);
        var timerImages = new TimerImages
        {
            outSideImage = parent.GetChild(0).GetComponent<Image>(),
            inSideImage = parent.GetChild(0).GetChild(0).gameObject.transform.GetComponent<Image>(),
            secondHandImage = parent.GetChild(0).GetChild(1).gameObject.transform.GetComponent<Image>(),
        };
        return timerImages;
    }

    async UniTask CountTime(GameObject timerObj,TimerImages timerImages,float waitTime)
    {
        var time = 0f;
        var originalRot = timerImages.secondHandImage.transform.localRotation;

        if(timerObj != null)
        {
            while (time < waitTime)
            {
                UIFuctions.LookToCamera(timerObj);
                time += Time.deltaTime;
                var ratio = time / waitTime;
                var rotateAmount = 360f * ratio;
                timerImages.outSideImage.fillAmount = ratio;
                timerImages.inSideImage.fillAmount = ratio;
                timerImages.secondHandImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -rotateAmount) * originalRot;
                await UniTask.Yield();
            }
        }      
    }
}
