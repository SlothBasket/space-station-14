namespace Content.Shared.Botany.Components;

/// <summary>
/// Causes a plant or its produce to electrocute entities on contact.
/// </summary>
[RegisterComponent]
public sealed partial class BotanyElectrifiedComponent : Component
{
    /// <summary>
    /// Shock damage dealt by the electrocution.
    /// </summary>
    [DataField]
    public int ShockDamage = 5;

    /// <summary>
    /// Duration of the electrocution in seconds.
    /// </summary>
    [DataField]
    public float ElectrocuteTime = 1f;
}
