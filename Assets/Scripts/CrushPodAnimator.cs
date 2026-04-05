using UnityEngine;
using DG.Tweening;

public class CrushPodAnimator : MonoBehaviour
{
    [Header("References")]
    public Transform crushPodModel; // child model for squash/stretch only

    [Header("Idle Settings")]
    public float idleSquashScale = 0.9f;   // how much Y compresses
    public float idleDuration = 1f;
    public float idleOffsetY = 0.05f;      // small downward offset so bottom stays grounded

    [Header("Attack Settings")]
    public float jumpPower = 2f;       // arc height
    public float jumpDuration = 0.6f;  // total jump time
    public int jumpCount = 1;          // single jump
    public float impactSquash = 0.7f;  // Y compression on impact
    public float impactOffsetY = 0.2f; // push down so it looks like top slams ground

    private Tween idleTween;
    private Vector3 startScale;
    private Vector3 startLocalPos;

    void Start()
    {
        startScale = crushPodModel.localScale;
        startLocalPos = crushPodModel.localPosition;
        PlayIdle();
    }

    public void PlayIdle()
    {
        idleTween?.Kill();

        // Idle squash: compress Y and move slightly down so bottom stays grounded
        idleTween = DOTween.Sequence()
            .Append(crushPodModel.DOScale(
                new Vector3(startScale.x, startScale.y * idleSquashScale, startScale.z),
                idleDuration
            ).SetEase(Ease.InOutSine))
            .Join(crushPodModel.DOLocalMoveY(startLocalPos.y - idleOffsetY, idleDuration).SetEase(Ease.InOutSine))
            .Append(crushPodModel.DOScale(startScale, idleDuration).SetEase(Ease.InOutSine))
            .Join(crushPodModel.DOLocalMoveY(startLocalPos.y, idleDuration).SetEase(Ease.InOutSine))
            .SetLoops(-1);
    }

    public void PlayAttack(Vector3 zombieWorldPos, System.Action onImpact = null)
    {
        idleTween?.Kill();

        // Clamp target to same lane (Z fixed to parent’s Z)
        Vector3 targetPos = new Vector3(
            zombieWorldPos.x,
            transform.position.y,
            transform.position.z
        );

        // Jump with arc directly to zombie’s X
        transform.DOJump(targetPos, jumpPower, jumpCount, jumpDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // Impact squash: compress Y and push down so it looks like top slams ground
                crushPodModel.DOScale(
                    new Vector3(startScale.x * 1.2f, startScale.y * impactSquash, startScale.z * 1.2f),
                    0.15f
                ).SetEase(Ease.OutQuad);

                crushPodModel.DOLocalMoveY(startLocalPos.y - impactOffsetY, 0.15f).SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    crushPodModel.DOScale(startScale, 0.2f).SetEase(Ease.InQuad);
                    crushPodModel.DOLocalMoveY(startLocalPos.y, 0.2f).SetEase(Ease.InQuad);
                    // Damage zombie
                    onImpact?.Invoke();

                    // Kill the plant properly (frees tile)
                    GetComponent<Squash>().DamagePlant();

                });
            });
    }
}
