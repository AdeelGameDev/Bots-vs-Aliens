using UnityEngine;
using DG.Tweening;

public class SunflowerAnimator : MonoBehaviour
{
    [Header("References")]
    public Transform sunflowerHead;

    [Header("Animation Settings")]
    public float idleBobHeight = 0.1f;
    public float idleDuration = 2f;
    public float produceScale = 1.2f;
    public float produceDuration = 0.3f;

    private Tween idleTween;
    private Vector3 startLocalPos;

    void Start()
    {
        startLocalPos = sunflowerHead.localPosition;
        PlayIdle();
    }

    public void PlayIdle()
    {
        idleTween?.Kill();

        // Bob relative to starting position
        idleTween = sunflowerHead.DOLocalMoveY(
            startLocalPos.y + idleBobHeight,
            idleDuration
        ).SetLoops(-1, LoopType.Yoyo)
         .SetEase(Ease.InOutSine);
    }

    public void StopIdle()
    {
        idleTween?.Kill();
        // Reset back to starting position
        sunflowerHead.DOLocalMoveY(startLocalPos.y, 0.3f).SetEase(Ease.OutSine);
    }

    public void PlayProduce()
    {
        StopIdle();

        // Scale pulse relative to starting scale
        sunflowerHead.DOScale(produceScale, produceDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                sunflowerHead.DOScale(1f, produceDuration)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        PlayIdle();
                    });
            });
    }
}
