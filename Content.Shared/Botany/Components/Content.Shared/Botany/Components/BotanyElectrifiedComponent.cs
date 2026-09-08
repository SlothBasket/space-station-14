using Content.Shared.Botany.Components;
using Content.Shared.Botany.Items.Components;

namespace Content.Shared.EntityEffects.Effects.Botany;

/// <summary>
/// Applies the electrified mutation to a living plant.
/// </summary>
public sealed partial class BotanyElectrifyPlantEntityEffectSystem
    : EntityEffectSystem<PlantComponent, BotanyElectrify>
{
    protected override void Effect(
        Entity<PlantComponent> entity,
        ref EntityEffectEvent<BotanyElectrify> args)
    {
        var electrified = EnsureComp<BotanyElectrifiedComponent>(entity.Owner);

        electrified.ShockDamage = args.Effect.ShockDamage;
        electrified.ElectrocuteTime = args.Effect.ElectrocuteTime;
        Dirty(entity.Owner, electrified);
    }
}

/// <summary>
/// Applies the electrified mutation to harvested produce.
/// </summary>
public sealed partial class BotanyElectrifyProduceEntityEffectSystem
    : EntityEffectSystem<ProduceComponent, BotanyElectrify>
{
    protected override void Effect(
        Entity<ProduceComponent> entity,
        ref EntityEffectEvent<BotanyElectrify> args)
    {
        var electrified = EnsureComp<BotanyElectrifiedComponent>(entity.Owner);

        electrified.ShockDamage = args.Effect.ShockDamage;
        electrified.ElectrocuteTime = args.Effect.ElectrocuteTime;
        Dirty(entity.Owner, electrified);
    }
}

public class BotanyElectrifiedComponent
{
}

/// <summary>
/// Electrifies a plant or its produce.
/// </summary>
public sealed partial class BotanyElectrify : EntityEffectBase<BotanyElectrify>
{
    [DataField]
    public int ShockDamage = 5;

    [DataField]
    public float ElectrocuteTime = 1f;
}
