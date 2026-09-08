using Content.Server.Electrocution;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Robust.Shared.Physics.Events;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Handles shocks caused by electrified plants and their produce.
/// </summary>
public sealed partial class BotanyElectrifiedSystem : EntitySystem
{
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly PlantTraySystem _plantTray = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Touching/colliding with an electrified plant or produce.
        SubscribeLocalEvent<BotanyElectrifiedComponent, StartCollideEvent>(OnCollide);

        // Picking up electrified produce.
        SubscribeLocalEvent<BotanyElectrifiedComponent, GotEquippedHandEvent>(OnPickedUp);

        // Using an item on a tray containing an electrified plant.
        SubscribeLocalEvent<PlantTrayComponent, InteractUsingEvent>(OnTrayInteractUsing);
    }

    private void OnCollide(
        Entity<BotanyElectrifiedComponent> ent,
        ref StartCollideEvent args)
    {
        Shock(
            args.OtherEntity,
            ent.Owner,
            ent.Comp);
    }

    private void OnPickedUp(
        Entity<BotanyElectrifiedComponent> ent,
        ref GotEquippedHandEvent args)
    {
        // Items held in a hand are parented to the entity holding them.
        var user = Transform(ent.Owner).ParentUid;

        Shock(
            user,
            ent.Owner,
            ent.Comp);
    }

    private void OnTrayInteractUsing(
        Entity<PlantTrayComponent> tray,
        ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // TryGetPlant expects Entity<PlantTrayComponent?>,
        // so convert the non-nullable tray entity with AsNullable().
        if (!_plantTray.TryGetPlant(tray.AsNullable(), out var nullablePlantUid))
            return;

        // Convert EntityUid? into a definite EntityUid.
        if (nullablePlantUid is not { } plantUid)
            return;

        // The plant in this tray isn't electrified.
        if (!TryComp<BotanyElectrifiedComponent>(plantUid, out var electrified))
            return;

        Shock(
            args.User,
            plantUid,
            electrified);

        // Prevent the tool/item interaction from continuing.
        args.Handled = true;
    }

    private void Shock(
        EntityUid target,
        EntityUid source,
        BotanyElectrifiedComponent electrified)
    {
        _electrocution.TryDoElectrocution(
            target,
            source,
            electrified.ShockDamage,
            TimeSpan.FromSeconds(electrified.ElectrocuteTime),
            refresh: true);
    }
}
