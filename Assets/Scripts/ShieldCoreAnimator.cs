using UnityEngine;
using DG.Tweening;

public class ShieldCoreAnimator : MonoBehaviour
{
    [Header("References")]
    public Transform shieldModel; // assign the whole model in Inspector

    [Header("Animation Settings")]
    public float damageShakeStrength = 0.2f;
    public int damageShakeVibrato = 10;
    public float damageShakeDuration = 0.3f;
    public float idlePulseMultiplier = 1.05f; // relative multiplier
    public float idleDuration = 3f;

    private Tween idleTween;
    private Vector3 startScale;

    void Start()
    {
        startScale = shieldModel.localScale; // capture original scale
        PlayIdle();
    }

    public void PlayIdle()
    {
        idleTween?.Kill();

        // Pulse relative to starting scale
        idleTween = shieldModel.DOScale(startScale * idlePulseMultiplier, idleDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    public void PlayDamage()
    {
        idleTween?.Pause();

        // Softer shake effect
        shieldModel.DOShakePosition(
            duration: damageShakeDuration,
            strength: new Vector3(damageShakeStrength * 0.5f, 0, 0), // reduced strength
            vibrato: damageShakeVibrato / 2,                         // fewer vibrations
            randomness: 45,                                          // less chaotic
            snapping: false,
            fadeOut: true
        ).OnComplete(() =>
        {
            idleTween?.Play();
        });
    }

}
