# DefModExtension Classes

Empire provides 7 DefModExtension classes and 1 extension interface that attach custom behavior to specific def types. Add them via the standard `modExtensions` list on any def.

```xml
<modExtensions>
    <li Class="FactionColonies.YourExtensionClass">
        <!-- fields here -->
    </li>
</modExtensions>
```

---

## FCEventHandlerExtension

**Attaches to**: `FCEventDef`
**Purpose**: Custom event resolution logic without Harmony patches.

**Class**: `FactionColonies.FCEventHandlerExtension` (extends `DefModExtension`)

| Virtual Method | Signature | When Called |
|----------------|-----------|------------|
| `ResolveEvent` | `bool ResolveEvent(FCEvent evt, FactionFC faction)` | When the event triggers. Return `true` to skip built-in resolution logic (loot, stat cleanup, etc. still run). Return `false` for normal processing. |
| `OnEventTriggered` | `void OnEventTriggered(FCEvent evt)` | After all standard processing (loot, stat cleanup, cascading events). Always called regardless of `ResolveEvent`'s return value. |
| `ShouldCancelOnSettlementRemoval` | `bool ShouldCancelOnSettlementRemoval(FCEvent evt, WorldSettlementFC settlement)` | When a settlement is removed. Return `true` to cancel this event. Default: `false`. |

See [Event System](event-system.md) for the full event lifecycle.

---

## BuildingFCExtension

**Attaches to**: `BuildingFCDef`
**Purpose**: Attach a custom `SettlementBuildingComp` to a building.

**Class**: `FactionColonies.BuildingFCExtension` (extends `DefModExtension`)

| Field | Type | Description |
|-------|------|-------------|
| `compClass` | `Type` | The C# class to instantiate. Must extend [SettlementBuildingComp](abstract-base-classes.md#settlementbuildingcomp). |

```xml
<modExtensions>
    <li Class="FactionColonies.BuildingFCExtension">
        <compClass>YourNamespace.YourBuildingComp</compClass>
    </li>
</modExtensions>
```

The comp is instantiated when the building is constructed and receives `Tick()`, `OnConstruct()`, `OnDeconstruct()`, and `GetGizmos()` callbacks.

---

## ResourceFilterExtension

**Attaches to**: `ResourceTypeDef`
**Purpose**: Define custom filters for resource item generation beyond simple allow/block lists.

**Class**: `FactionColonies.ResourceFilterExtension` (abstract, extends `DefModExtension`)

| Virtual Method | Signature | Description |
|----------------|-----------|-------------|
| `SetFilter` | `void SetFilter(ThingFilter filter, TechLevel techlevel, ResourceFC resource = null)` | Modify the `ThingFilter` used to determine valid items for this resource. |
| `GetThingSetMaker` | `ThingSetMaker GetThingSetMaker(out TechLevel tlevel, ResourceFC resource = null)` | Return a custom `ThingSetMaker` for item generation. Return `null` to use default. |
| `GenerateSpecificThings` | `List<Thing> GenerateSpecificThings(ThingDef thingDef, int quantity, QualityCategory quality, ThingDef stuffDef, ResourceFC resource = null)` | Generate specific items by def. Used when the player selects specific tithes. |

**Base mod example**: `ResourceFilterExtension_Animals` — filters for animal race ThingDefs and generates animal pawns instead of items.

---

## ResourceProductionExtension

**Attaches to**: `ResourceTypeDef`
**Purpose**: Add conditional production bonuses based on terrain or settlement properties.

**Class**: `FactionColonies.ResourceProductionExtension` (abstract, extends `DefModExtension`)

| Field | Type | Description |
|-------|------|-------------|
| `extName` | `string` | Display name for this extension in tooltips. |
| `extDesc` | `string` | Description text. |

| Virtual Method | Signature | No-op return | Description |
|----------------|-----------|-------------|-------------|
| `GetAdditiveBonus` | `double GetAdditiveBonus(PlanetTile tile, WorldSettlementFC settlement = null)` | `0` | Additive production bonus based on tile/settlement. |
| `GetMultiplierBonus` | `double GetMultiplierBonus(PlanetTile tile, WorldSettlementFC settlement = null)` | `1` | Multiplicative bonus based on tile/settlement. |

These values feed into the resource production formula's "dictionary" sources (the static/environmental layer). See [Stat System — Resource Production Formula](stat-system.md#resource-production-formula).

**Base mod example**: `ResourceProductionExtension_Hilliness` — provides additive/multiplicative bonuses per hilliness level (Flat, SmallHills, LargeHills, Mountainous).

---

## ResourcePoolExtension

**Attaches to**: `ResourceTypeDef` (pool resources only, where `isPoolResource = true`)
**Purpose**: Define how a faction-level resource pool is created and managed.

**Class**: `FactionColonies.ResourcePoolExtension` (abstract, extends `DefModExtension`)

| Virtual Method | Signature | Description |
|----------------|-----------|-------------|
| `CreatePool` | `double CreatePool(double production, WorldSettlementFC settlement = null)` | Convert raw production to pool value. Called per settlement during tax collection. |
| `ResetAtTaxTime` | `bool ResetAtTaxTime()` | If true, pool resets to 0 each tax cycle. Default: `false`. |
| `PreAddToGlobalPool` | `double PreAddToGlobalPool(double value)` | Transform a value before adding to the global pool. Return the modified value. |
| `AddedToGlobalPool` | `void AddedToGlobalPool(double value)` | Called after a value has been added to the global pool. |
| `GetFactionMenuFloatMenuOptions` | `IEnumerable<FloatMenuOption> GetFactionMenuFloatMenuOptions(ResourcePool pool)` | Return float menu options for the faction-level resource pool UI. |
| `DailyUpdate` | `void DailyUpdate(ResourcePool pool)` | Called once per in-game day. Use for decay, regeneration, or other time-based pool logic. |

**Base mod examples**:
- `ResourcePoolExt_Power` — scales pool by 100x.
- `ResourcePoolExt_Research` — manages research point accumulation with daily updates.

---

## ResourceTaxExtension

**Attaches to**: `ResourceTypeDef` (non-pool resources only)
**Purpose**: Hook into tithe/tax generation for non-pool resources.

**Class**: `FactionColonies.ResourceTaxExtension` (abstract, extends `DefModExtension`)

| Virtual Method | Signature | Description |
|----------------|-----------|-------------|
| `OnPreTaxGeneration` | `void OnPreTaxGeneration(ResourceFC resource, WorldSettlementFC settlement)` | Called before tithe items are generated for this resource at this settlement. |
| `OnPostTaxGeneration` | `void OnPostTaxGeneration(ResourceFC resource, WorldSettlementFC settlement, List<Thing> generatedThings, ref int extraSilver)` | Called after tithe items are generated. You can modify `generatedThings` or adjust `extraSilver` (ref). |

---

## SettlementTypeExtension

**Attaches to**: `WorldSettlementDef` (**required** — every WorldSettlementDef must have one)
**Purpose**: Controls the entire settlement lifecycle — creation, naming, upgrades, tax delivery, destruction.

**Class**: `FactionColonies.SettlementTypeExtension` (extends `DefModExtension`)

The base class provides sensible defaults for surface settlements. Subclass it for custom settlement types.

### Lifecycle Hooks

| Virtual Method | Signature | Description |
|----------------|-----------|-------------|
| `PreCreation` | `void PreCreation(ref PlanetTile tile, ref WorldSettlementDef settlementType)` | Called before settlement creation. Can modify tile or settlement type. |
| `PostCreation` | `void PostCreation(WorldSettlementFC settlement)` | Called after settlement is created and fully initialized. |
| `OnUpgrade` | `void OnUpgrade(WorldSettlementFC settlement, int oldLevel, int newLevel)` | Called when settlement level increases. |
| `PreTypeTransition` | `void PreTypeTransition(WorldSettlementFC settlement, WorldSettlementDef newDef)` | Called before a settlement changes type. |
| `PostTypeTransition` | `void PostTypeTransition(WorldSettlementFC settlement, WorldSettlementDef oldDef)` | Called after a settlement changes type. |
| `PreDestruction` | `void PreDestruction(WorldSettlementFC settlement)` | Called before a settlement is destroyed. |

### Tax Hooks

| Virtual Method | Signature | Description |
|----------------|-----------|-------------|
| `PreTax` | `void PreTax(WorldSettlementFC settlement)` | Called before tax collection for this settlement. |
| `PostTax` | `void PostTax(WorldSettlementFC settlement, ref int silverAmount, List<Thing> titheThings)` | Called after tax collection. Can modify `silverAmount` (ref) and `titheThings`. |

### Validation

| Virtual Method | Signature | Description |
|----------------|-----------|-------------|
| `TileIsValidForSettlement` | `bool TileIsValidForSettlement(PlanetTile tile, StringBuilder reason = null)` | Return false to reject a tile. Append to `reason` for player feedback. |
| `TileIsValidForTypeTransition` | `bool TileIsValidForTypeTransition(PlanetTile tile, StringBuilder reason = null)` | Return false to prevent type change at this tile. |

### Configuration

| Virtual Method | Signature | Description |
|----------------|-----------|-------------|
| `GetSettlementName` | `string GetSettlementName(string fallback = "Settlement")` | Return a custom settlement name. Default: random from name database. |
| `GetCreationCost` | `int GetCreationCost()` | Silver cost to create this settlement. |
| `GetCreationTime` | `int GetCreationTime(PlanetTile destination)` | Ticks to create (travel/construction time). |
| `GetLocationText` | `string GetLocationText(WorldSettlementFC settlement)` | Location description for UI. |
| `GetTaxDeliveryMode` | `TaxDeliveryMode GetTaxDeliveryMode(bool canUseShuttle, PlanetTile sourceTile)` | How taxes are delivered (caravan, drop pod, shuttle). |
| `GetSettlementLevelDesc` | `string GetSettlementLevelDesc(int level)` | Description text for a given level. |
| `GetBuildingSlots` | `int GetBuildingSlots(int level, int maxCount)` | Number of building slots at a given level. |
| `GetUpgradeCost` | `int GetUpgradeCost(int level, int baseCost)` | Silver cost to upgrade to a given level. |
| `GetUpgradeTime` | `int GetUpgradeTime(int level, double buildTimeMult)` | Ticks to upgrade to a given level. |
| `GetTileForSettlement` | `PlanetTile GetTileForSettlement(PlanetTile tile)` | Transform or replace the tile used for settlement. |

**Base mod example**: `SettlementTypeExtension_Orbital` — custom naming with space-themed keywords, forced drop pod/shuttle delivery, orbital tile validation, custom creation cost/time.

---

## IBuildingDetailSection

**Attaches to**: `BuildingFCDef`
**Purpose**: Add custom sections to the building detail panel in the building construction window.

Unlike the other entries on this page, `IBuildingDetailSection` is an **interface**, not a class. Implement it on a `DefModExtension` subclass attached to a `BuildingFCDef`. The window discovers implementors via `def.modExtensions.OfType<IBuildingDetailSection>()`. Sections render between the Modifiers block and the Settlement Impact block.

```csharp
public interface IBuildingDetailSection
{
    string SectionLabel { get; }
    float GetSectionHeight(BuildingFCDef def, float width);
    void DrawSection(BuildingFCDef def, Rect contentRect);
    string GetCardDescription(BuildingFCDef def);
}
```

| Member | Description |
|--------|-------------|
| `SectionLabel` | Header label for the section (rendered by the caller in standard style). |
| `GetSectionHeight` | Total height needed for section content (excluding the header). Return `0` to hide the section entirely. |
| `DrawSection` | Draws section content into `contentRect`. The header is drawn by the caller; implementors only draw below it. |
| `GetCardDescription` | Short description appended to building card text in the left panel and tooltips. Return `null` or `""` to add nothing. |

```xml
<FactionColonies.BuildingFCDef>
    <defName>MyBuilding</defName>
    <modExtensions>
        <li Class="YourNamespace.MyBuildingDetailSection" />
    </modExtensions>
    <!-- ... -->
</FactionColonies.BuildingFCDef>
```

```csharp
public class MyBuildingDetailSection : DefModExtension, IBuildingDetailSection
{
    public string SectionLabel => "Custom Info";

    public float GetSectionHeight(BuildingFCDef def, float width) => 30f;

    public void DrawSection(BuildingFCDef def, Rect contentRect)
    {
        Widgets.Label(contentRect, "Custom content here");
    }

    public string GetCardDescription(BuildingFCDef def) => "Has custom info";
}
```