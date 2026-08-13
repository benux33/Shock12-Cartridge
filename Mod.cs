using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Modding.Custom;

namespace Shock12;

public sealed record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.bensburnedwaffles.shock12";
    public string Name { get; init; } = "12/70 Shock-12 Cartridge";
    public string Author { get; init; } = "BensBurnedWaffles";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("1.0.2");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.2");
    public bool HasPrepatcher { get; init; }
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; }
    public string License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.RagfairCallbacks - 1)]
public sealed class Shock12Mod : IOnLoad
{
    public const string ShockTemplateId = "76a0e2d8c1464f22a2c5fb01";

    private const string FtxTemplateId = "5d6e68e6a4b9361c140bcfe0";
    private const string TraderOfferId = "76a0e2d8c1464f22a2c5fb02";
    private const string AmmoParentId = "5485a8684bdc2da71d8b4567";
    private const string AmmoHandbookParentId = "5b47574386f77428ca22b33b";
    private const string JaegerId = "5c0647fdd443bc2504c2d371";
    private const string RoubleId = "5449016a4bdc2d6f028b456f";
    private const int Price = 900;

    private readonly CustomItemService _customItemService;
    private readonly TemplateTable _templates;
    private readonly TradersTable _traders;

    public Shock12Mod(
        CustomItemService customItemService,
        TemplateTable templates,
        TradersTable traders)
    {
        _customItemService = customItemService;
        _templates = templates;
        _traders = traders;
    }

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CreateCartridge();

        if (AddToFtxCompatibleSlots() == 0)
        {
            throw new InvalidOperationException(
                "Shock-12 could not find an FTX-compatible chamber or magazine.");
        }

        AddJaegerOffer();
        return Task.CompletedTask;
    }

    private void CreateCartridge()
    {
        NewItemFromCloneDetails details = new()
        {
            NewItemName = "patron_12x70_shock_12",
            ItemTplToClone = FtxTemplateId,
            ParentId = AmmoParentId,
            NewId = ShockTemplateId,
            HandbookParentId = AmmoHandbookParentId,
            HandbookPriceRoubles = Price,
            FleaPriceRoubles = Price * 1.25,
            AddToHandbook = true,
            AddToFleaPriceDb = true,
            AddToWeaponShelf = false,
            Locales = new Dictionary<string, LocaleDetails>
            {
                ["en"] = new LocaleDetails
                {
                    Name = "12/70 Shock-12 cartridge",
                    ShortName = "Shock-12",
                    Description =
                        "A 12/70 less-lethal shock cartridge designed to incapacitate through severe blunt trauma. "
                        + "A successful hit causes pain for 60 seconds, concussion for 30 seconds, an extreme "
                        + "15-second hand tremor that makes precise aiming nearly impossible, and a 5-second panic "
                        + "attack. The impact also immediately exhausts body and arm stamina. The projectile deals "
                        + "25 damage with 1 penetration and has no fragmentation, light-bleed, or heavy-bleed chance.",
                },
            },
            OverrideProperties = new TemplateItemProperties
            {
                AmmoType = "bullet",
                ProjectileCount = 1,
                BuckshotBullets = 0,
                Damage = 25,
                PenetrationPower = 1,
                BulletDiameterMilimeters = 13,
                BulletMassGram = 19.4,
                InitialSpeed = 320,
                BallisticCoeficient = 0.188,
                SpeedRetardation = 0.00013,
                ArmorDamage = 1,
                FragmentationChance = 0,
                RicochetChance = 0,
                PenetrationChanceObstacle = 0,
                PenetrationDamageMod = 0,
                HeavyBleedingDelta = 0,
                LightBleedingDelta = 0,
                StaminaBurnPerDamage = 4,
                AmmoAccr = -20,
                AmmoRec = -10,
                StackMaxSize = 20,
            },
        };

        CreateItemResult result = _customItemService.CreateItemFromClone(details);
        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Shock-12 cartridge creation failed: {string.Join("; ", result.Errors)}");
        }
    }

    private int AddToFtxCompatibleSlots()
    {
        MongoId sourceId = FtxTemplateId;
        MongoId newId = ShockTemplateId;
        int changedFilters = 0;

        foreach (TemplateItem item in _templates.Items.Values)
        {
            TemplateItemProperties? properties = item.Properties;
            if (properties is null)
            {
                continue;
            }

            changedFilters += AddToSlots(properties.Slots, sourceId, newId);
            changedFilters += AddToSlots(properties.Chambers, sourceId, newId);
            changedFilters += AddToSlots(properties.Cartridges, sourceId, newId);

            if (properties.StackSlots is null)
            {
                continue;
            }

            foreach (StackSlot stackSlot in properties.StackSlots)
            {
                changedFilters += AddToFilters(stackSlot.Properties?.Filters, sourceId, newId);
            }
        }

        return changedFilters;
    }

    private static int AddToSlots(
        IEnumerable<Slot>? slots,
        MongoId sourceId,
        MongoId newId)
    {
        if (slots is null)
        {
            return 0;
        }

        int changedFilters = 0;
        foreach (Slot slot in slots)
        {
            changedFilters += AddToFilters(slot.Properties?.Filters, sourceId, newId);
        }

        return changedFilters;
    }

    private static int AddToFilters(
        IEnumerable<SlotFilter>? filters,
        MongoId sourceId,
        MongoId newId)
    {
        if (filters is null)
        {
            return 0;
        }

        int changedFilters = 0;
        foreach (SlotFilter filter in filters)
        {
            HashSet<MongoId>? acceptedItems = filter.Filter;
            if (acceptedItems is not null && acceptedItems.Contains(sourceId) && acceptedItems.Add(newId))
            {
                changedFilters++;
            }
        }

        return changedFilters;
    }

    private void AddJaegerOffer()
    {
        if (!_traders.TryGetValue(JaegerId, out Trader? jaeger))
        {
            throw new InvalidOperationException("Shock-12 could not find Jaeger in the trader database.");
        }

        MongoId offerId = TraderOfferId;
        jaeger.Assort.Items.Add(new Item
        {
            Id = offerId,
            Template = ShockTemplateId,
            ParentId = "hideout",
            SlotId = "hideout",
            Upd = new Upd
            {
                UnlimitedCount = true,
                StackObjectsCount = 9_999_999,
                BuyRestrictionMax = 50,
                BuyRestrictionCurrent = 0,
            },
        });

        jaeger.Assort.BarterScheme[offerId] = new List<List<BarterScheme>>
        {
            new()
            {
                new BarterScheme
                {
                    Count = Price,
                    Template = RoubleId,
                },
            },
        };
        jaeger.Assort.LoyalLevelItems[offerId] = 1;
    }
}
