public class WallNut : Plant
{
    private ShieldCoreAnimator animator;
    private CodeMonkey.HealthSystemCM.HealthSystem healthSystem;

    protected override void Start()
    {
        base.Start();
        animator = GetComponent<ShieldCoreAnimator>();
        healthSystem = GetComponent<CodeMonkey.HealthSystemCM.HealthSystemComponent>().GetHealthSystem();

        // Subscribe to damage event
        healthSystem.OnDamaged += HealthSystem_OnDamaged;
    }

    private void HealthSystem_OnDamaged(object sender, System.EventArgs e)
    {
        if (animator != null)
            animator.PlayDamage();
    }
}
