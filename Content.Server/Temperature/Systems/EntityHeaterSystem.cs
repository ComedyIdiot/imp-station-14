using Content.Server.Atmos.Components;
using Content.Server.Power.Components;
using Content.Shared.Placeable;
using Content.Shared.Temperature;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;

namespace Content.Server.Temperature.Systems;

/// <summary>
/// Handles the server-only parts of <see cref="SharedEntityHeaterSystem"/>
/// </summary>
public sealed class EntityHeaterSystem : SharedEntityHeaterSystem
{
    [Dependency] private readonly TemperatureSystem _temperature = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityHeaterComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<EntityHeaterComponent> ent, ref MapInitEvent args)
    {
        // Set initial power level
        if (TryComp<ApcPowerReceiverComponent>(ent, out var power))
            power.Load = SettingPower(ent.Comp.Setting, ent.Comp.Power);
    }

    public override void Update(float deltaTime)
    {
        var query = EntityQueryEnumerator<EntityHeaterComponent, ItemPlacerComponent>(); //Imp edit
        while (query.MoveNext(out var uid, out var comp, out var placer))
        {
            var energy = comp.Power * deltaTime; //Just use base power if no power required
            if (comp.RequirePower)
            {
                TryComp<ApcPowerReceiverComponent>(uid, out var power);
                if (power == null)
                    continue;
                if (!power.Powered)
                    continue;

                // don't divide by total entities since its a big grill
                // excess would just be wasted in the air but that's not worth simulating
                // if you want a heater thermomachine just use that...
                energy = power.PowerReceived * deltaTime;
            }

            if (TryComp<FlammableComponent>(uid, out var flammable) && !flammable.OnFire)
                continue;


            foreach (var ent in placer.PlacedEntities)
            {
                _temperature.ChangeHeat(ent, energy);
            }
        }
    }

    /// <remarks>
    /// <see cref="ApcPowerReceiverComponent"/> doesn't exist on the client, so we need
    /// this server-only override to handle setting the network load.
    /// </remarks>
    protected override void ChangeSetting(Entity<EntityHeaterComponent> ent, EntityHeaterSetting setting, EntityUid? user = null)
    {
        base.ChangeSetting(ent, setting, user);

        if (!TryComp<ApcPowerReceiverComponent>(ent, out var power))
            return;

        power.Load = SettingPower(setting, ent.Comp.Power);
    }
}
