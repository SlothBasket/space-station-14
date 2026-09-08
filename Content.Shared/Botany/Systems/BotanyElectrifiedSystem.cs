using Content.Shared.Botany.Components;
using Content.Shared.Botany.Traits.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.StatusEffect;
using Content.Shared.Throwing;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;
using Content.Shared.Botany.Events;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Handles electrical contact with mutated plants and produce.
/// Predicts blocked interactions on the client; applies shocks on the server.
/// </summary>
public sealed partial class BotanyElectrifiedSystem : EntitySystem
{
    [Dependency] private SharedElectrocutionSystem _electrocution = default!;
    [Dependency] private PlantTraySystem _plantTray = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private PlantSystem _plant = default!;

    // Collision and thrown-hit events can report the same contact in one tick.
    // Record attempts before shocking to also prevent reentrant shocks.
    private readonly Dictionary<(EntityUid Source, EntityUid Target), bool> _shockResults = new();
    private TimeSpan _shockTime;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BotanyElectrifiedComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<BotanyElectrifiedComponent, ThrowDoHitEvent>(OnThrowHit);
        SubscribeLocalEvent<BotanyElectrifiedComponent, BeforeGettingEquippedHandEvent>(OnBeforePickup);
        SubscribeLocalEvent<BotanyElectrifiedComponent, InteractUsingEvent>(OnInteractUsing,
            before: [typeof(PlantTraitLigneousSystem)]);
        SubscribeLocalEvent<PlantTrayComponent, InteractUsingEvent>(OnTrayInteractUsing,
            before: [typeof(PlantTraitLigneousSystem)]);
        SubscribeLocalEvent<BotanyElectrifiedComponent, InteractHandEvent>(OnInteractHand,
            before: [typeof(PlantHarvestSystem)]);
        SubscribeLocalEvent<BotanyElectrifiedComponent, PlantHarvestAttemptEvent>(OnHarvestAttempt,
            before: [typeof(PlantTraitLigneousSystem)]);
    }

    private void OnCollide(Entity<BotanyElectrifiedComponent> ent, ref StartCollideEvent args)
    {
        if (_net.IsServer)
            TryShock(args.OtherEntity, ent);
    }

    private void OnThrowHit(Entity<BotanyElectrifiedComponent> ent, ref ThrowDoHitEvent args)
    {
        if (_net.IsServer)
            TryShock(args.Target, ent);
    }

    private void OnBeforePickup(Entity<BotanyElectrifiedComponent> ent, ref BeforeGettingEquippedHandEvent args)
    {
        if (!args.Cancelled && TryShock(args.User, ent))
            args.Cancelled = true;
    }

    private void OnInteractUsing(Entity<BotanyElectrifiedComponent> ent, ref InteractUsingEvent args)
    {
        if (!args.Handled && TryShock(args.User, ent))
            args.Handled = true;
    }

    private void OnTrayInteractUsing(Entity<PlantTrayComponent> ent, ref InteractUsingEvent args)
    {
        if (!args.Handled && TryShockFromTray(args.User, ent))
            args.Handled = true;
    }

    private void OnInteractHand(
        Entity<BotanyElectrifiedComponent> ent,
        ref InteractHandEvent args)
    {
        if (!args.Handled && TryShock(args.User, ent))
            args.Handled = true;
    }

    private void OnHarvestAttempt(
        Entity<BotanyElectrifiedComponent> ent,
        ref PlantHarvestAttemptEvent args)
    {
        if (!args.Cancelled && TryShock(args.User, ent))
            args.Cancelled = true;
    }

    private void OnTrayInteractHand(Entity<PlantTrayComponent> ent, ref InteractHandEvent args)
    {
        if (!args.Handled && TryShockFromTray(args.User, ent))
            args.Handled = true;
    }

    private bool TryShockFromTray(EntityUid user, Entity<PlantTrayComponent> tray)
    {
        if (!_plantTray.TryGetPlant(tray.AsNullable(), out var nullablePlant)
            || nullablePlant is not { } plant
            || !TryComp<BotanyElectrifiedComponent>(plant, out var electrified))
            return false;

        return TryShock(user, (plant, electrified));
    }

    private bool TryShock(EntityUid target, Entity<BotanyElectrifiedComponent> source)
    {
        if (TerminatingOrDeleted(target) || TerminatingOrDeleted(source.Owner))
            return false;

        if (_net.IsClient)
        {
            // Predict only whether contact is blocked. The real electrocution
            // system is server-only. Use its normal insulation relay, including
            // the same exclusion of pocket contents, rather than checking gloves.
            var attempt = new ElectrocutionAttemptEvent(target, source.Owner, 1f, ~SlotFlags.POCKET);
            RaiseLocalEvent(target, attempt, true);

            return !attempt.Cancelled
                && attempt.SiemensCoefficient > 0
                && (int) (source.Comp.ShockDamage * attempt.SiemensCoefficient) > 0
                && _statusEffects.CanApplyEffect(target, "Electrocution");
        }

        if (_shockTime != _timing.CurTime)
        {
            _shockTime = _timing.CurTime;
            _shockResults.Clear();
        }

        var key = (source.Owner, target);
        if (_shockResults.TryGetValue(key, out var previousResult))
            return previousResult;

        // While the call is running, block any nested contact with this source.
        _shockResults[key] = true;
        var shocked = _electrocution.TryDoElectrocution(
            target,
            source.Owner,
            source.Comp.ShockDamage,
            TimeSpan.FromSeconds(source.Comp.ElectrocuteTime),
            refresh: true);
        _shockResults[key] = shocked;

        if (shocked)
            ShowTraySparks(source.Owner);

        return shocked;
    }

    /// <summary>
    /// Shows sparks at the tray after a living plant successfully shocks someone.
    /// </summary>
    private void ShowTraySparks(EntityUid source)
    {
        if (!TryComp<PlantHolderComponent>(source, out var holder) || holder.Dead)
            return;

        if (!_plant.TryGetTray(source, out var tray))
            return;

        Spawn("EffectBotanyTraySparks", Transform(tray.Owner).Coordinates);
    }
}
