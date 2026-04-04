using UnityEngine;
using DG.Tweening;

public class CannonAnimator : MonoBehaviour
{
    [Header("References")]
    public Transform cannonHead;

    [Header("Animation Settings")]
    public float idleRotationAngle = 10f;
    public float idleDuration = 2f;
    public float shootRecoilDistance = 0.3f;
    public float shootDuration = 0.2f;
    public float deathFallDistance = 2f;
    public float deathDuration = 1f;

    private Tween idleTween;

    public void PlayIdle()
    {
        if (idleTween != null && idleTween.IsActive()) return;

        idleTween = cannonHead.DOLocalRotate(
            new Vector3(0, idleRotationAngle, 0),
            idleDuration
        ).SetLoops(-1, LoopType.Yoyo)
         .SetEase(Ease.InOutSine);
    }

    public void StopIdleAndPointForward()
    {
        idleTween?.Kill();
        cannonHead.DOLocalRotate(Vector3.zero, 0.3f).SetEase(Ease.OutSine);
    }

    public void PlayShoot()
    {
        cannonHead.DOLocalMoveZ(-shootRecoilDistance, shootDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                cannonHead.DOLocalMoveZ(0, shootDuration).SetEase(Ease.InQuad);
            });
    }

    public void PlayDeath()
    {
        DOTween.Kill(cannonHead);
        cannonHead.DOLocalMoveY(-deathFallDistance, deathDuration).SetEase(Ease.InQuad);
        cannonHead.DOLocalRotate(new Vector3(90, 0, 0), deathDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutBounce);
    }
}
