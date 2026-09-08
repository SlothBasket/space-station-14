using Robust.Shared.GameStates;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Causes a plant or its produce to electrocute entities on contact.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BotanyElectrifiedComponent : Component
{
    /// <summary>
    /// Shock damage dealt by the electrocution.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ShockDamage = 5;

    /// <summary>
    /// Duration of the electrocution in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ElectrocuteTime = 1f;
}
