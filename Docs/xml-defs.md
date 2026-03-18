# XML Def Types Reference

Empire defines 12 custom def types. All support `modExtensions` for attaching [DefModExtensions](def-mod-extensions.md). Annotated XML examples for every def type are in [ExampleDefs/](ExampleDefs/).

---

## Primary Defs

These two defs govern settlements and resources — the core systems of Empire.

### ResourceTypeDef

**Class**: `FactionColonies.ResourceTypeDef` (extends `Def`)

Defines a resource category (e.g., Food, Weapons, Mining). Resources are the primary economic output of settlements. Each resource has filter rules controlling what items can be tithed, production stats linking it to the [stat system](stat-system.md), and optional extensions for specialized behavior.

**To define a new resource end-to-end, you need:**
1. A `ResourceTypeDef` (this def)
2. Two `FCStatDef` entries — one Additive, one Multiplicative — linked via `productionAdditiveStat`/`productionMultiplierStat`. See [Defining Production Stats](stat-system.md#defining-production-stats-for-a-new-resource).
3. Entries in `BiomeResourceDef` defs to set per-biome availability (or rely on `defaultBiomeAdditive`/`defaultBiomeMultiplier`)
4. Optionally, `modExtensions` for custom filters, production bonuses, pools, or tax hooks

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `iconPath` | `string` | — | Path to the resource icon texture (without extension). |
| `uiPriority` | `int` | `10000` | Sort order in resource lists. Lower = higher in list. |
| `minTechLevel` | `TechLevel` | `Undefined` | Minimum faction tech level to unlock this resource. |
| `maxTechLevel` | `TechLevel` | `Undefined` | Maximum faction tech level where this resource is available. |
| `researchProjectDefs` | `List<ResearchProjectDef>` | `[]` | Research projects required to unlock this resource. |
| `associatedSkills` | `List<SkillDef>` | `[]` | Pawn skills relevant to producing this resource. |
| `needsAllResearchRequirements` | `bool` | `true` | If true, both tech level AND research requirements must be met. |
| `isPoolResource` | `bool` | `false` | If true, this resource is a point pool (like Research, Power) instead of generating items. Requires a `ResourcePoolExtension`. |
| `isDefaultResource` | `bool` | `false` | If true, automatically included in any `WorldSettlementDef` with `defaultResources = true`. |
| `defaultBiomeAdditive` | `double` | `1` | Base additive production for biomes that don't specify this resource. |
| `defaultBiomeMultiplier` | `double` | `1` | Base multiplier for biomes that don't specify this resource. |
| `productionAdditiveStat` | `FCStatDef` | — | **Required.** The Additive FCStatDef for production bonuses. |
| `productionMultiplierStat` | `FCStatDef` | — | **Required.** The Multiplicative FCStatDef for production multipliers. |
| `titheMinCount` | `int` | `1` | Minimum item count when generating tithes. |
| `titheMaxCountBase` | `int` | `1` | Base maximum item count for tithes. |
| `titheMaxCountScaler` | `int` | `1` | Scales max count with production level. |
| `defenseWeight` | `float` | `0` | How much this resource's production contributes to settlement defense. |
| `thingAllowList` | `List<ThingAllowEntry>` | `[]` | Individual Things always allowed for tithing (with optional tech/research gates). |
| `thingBlockList` | `List<ThingDef>` | `[]` | Individual Things always blocked. Overrides allow lists. |
| `thingCategoryAllowList` | `List<ThingCategoryAllowEntry>` | `[]` | Thing categories to allow (with optional tech/research gates). |
| `thingCategoryBlockList` | `List<ThingCategoryDef>` | `[]` | Thing categories to block. Overrides category allow list. |
| `stuffCategoryAllowList` | `List<StuffCategoryDef>` | `[]` | Stuff categories to allow. |
| `stuffCategoryBlockList` | `List<StuffCategoryDef>` | `[]` | Stuff categories to block. |
| `biomeAllowList` | `List<BiomeResourceDef>` | `[]` | If set, resource only available in these biomes. Mutually exclusive with `biomeBlockList`. |
| `biomeBlockList` | `List<BiomeResourceDef>` | `[]` | If set, resource available everywhere except these biomes. |

**Filter priority** (highest to lowest): `thingBlockList` > `thingAllowList` > `thingCategoryBlockList` > `thingCategoryAllowList` / `stuffCategoryBlockList` > `stuffCategoryAllowList`.

**Supported modExtensions**: [ResourceFilterExtension](def-mod-extensions.md#resourcefilterextension), [ResourceProductionExtension](def-mod-extensions.md#resourceproductionextension), [ResourcePoolExtension](def-mod-extensions.md#resourcepoolextension), [ResourceTaxExtension](def-mod-extensions.md#resourcetaxextension).

See [ExampleDefs/ResourceTypeDef.xml](ExampleDefs/ResourceTypeDef.xml) for a full annotated example.

---

### WorldSettlementDef

**Class**: `FactionColonies.WorldSettlementDef` (extends `WorldObjectDef`)

Defines a settlement type (e.g., Surface, Orbital). Controls resource availability, worker caps, biome restrictions, and lifecycle behavior. Every WorldSettlementDef **must** include a `SettlementTypeExtension` (or subclass) in its `modExtensions`. The default extension should be suitable for most cases; only write a subclass if you need unique functionality.

**Key relationships:**
- `resources` list defines per-resource availability and base production values for this settlement type
- `defaultResources = true` auto-includes all ResourceTypeDefs with `isDefaultResource = true`; explicit `resources` entries override defaults
- `SettlementTypeExtension` controls the entire lifecycle — creation, upgrades, naming, tax delivery, destruction
- `statModifiers` provide static stat bonuses inherent to this settlement type
- `biomeResourceOverride` replaces the tile's natural biome resources (used for Orbital settlements)

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `resources` | `List<ResourceAvailability>` | `[]` | Per-resource `{resourceDef, additive, multiplier}` entries. |
| `defaultResources` | `bool` | `false` | Auto-include all default resources. Explicit entries take priority. |
| `workersMaxBase` | `int` | `0` | Base worker cap: `max = workersMaxBase + (workersMaxMult * level)`. |
| `workersMaxMult` | `int` | `3` | Worker cap multiplier per level. |
| `workersUltraMaxBase` | `int` | `5` | Hard cap base: `ultraMax = workersUltraMaxBase + (workersUltraMaxMult * level)`. |
| `workersUltraMaxMult` | `int` | `0` | Hard cap multiplier per level. |
| `allowedBiomes` | `List<BiomeDef>` | `[]` | If set, settlement only placeable in these biomes. Mutually exclusive with `blockedBiomes`. |
| `blockedBiomes` | `List<BiomeDef>` | `[]` | If set, settlement placeable everywhere except these biomes. |
| `statModifiers` | `List<FCStatModifier>` | `[]` | Static stat bonuses for this settlement type. |
| `biomeResourceOverride` | `BiomeResourceDef` | `null` | Overrides the tile's natural biome resources. |
| `researchProjects` | `List<ResearchProjectDef>` | `[]` | All must be completed to unlock this settlement type. |
| `techLevel` | `TechLevel` | `Undefined` | Minimum faction tech level required. |
| `planetLayers` | `List<PlanetLayerDef>` | `[]` | Planet layers this settlement can exist on (for non-surface types). |
| `maxSettlementLevel` | `int` | `99` | Hard cap on settlement level. |
| `maxBuildingCount` | `int` | `99` | Hard cap on building slots. |
| `titleKey` | `string` | `null` | Key for settlement-type-specific town titles. Falls back to default titles. |
| `isConstructed` | `bool` | `false` | If true, creation timer labeled "Construction Time" instead of "Travel Time". |
| `accentColor` | `Color?` | `null` | UI accent color for this settlement type. |
| `baseSettlementType` | `WorldSettlementDef` | `null` | Parent settlement type for inheritance-aware building allow/block list checks. When set, a building's allow/block list will match this def and all ancestors in the chain. |

**Required modExtension**: [SettlementTypeExtension](def-mod-extensions.md#settlementtypeextension).

See [ExampleDefs/WorldSettlementDef.xml](ExampleDefs/WorldSettlementDef.xml) for a full annotated example.

---

## Secondary Defs

### FCStatDef

Defines a named stat. See [Stat System](stat-system.md) for the full aggregation pipeline and how to define production stats for new resources.

**Class**: `FactionColonies.FCStatDef` (extends `Def`)

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `aggregation` | `FCStatAggregation` | `Additive` | `Additive` (sum) or `Multiplicative` (product). |
| `appliesToSettlements` | `bool` | `true` | Whether settlement-level sources contribute. |
| `descriptionKey` | `string` | `null` | Translation key for UI display. Receives bonus value as `{0}`. |
| `invertedForDisplay` | `bool` | `false` | If true, lower = better in UI coloring. |
| `linkedResource` | `ResourceTypeDef` | `null` | Links this stat to a resource for UI formatting. |

See [ExampleDefs/FCStatDef.xml](ExampleDefs/FCStatDef.xml).

### FCStatModifier

Not a Def — a helper class used in `statModifiers` lists on many defs.

| Field | Type | Description |
|-------|------|-------------|
| `stat` | `FCStatDef` | The stat to modify (by defName). |
| `value` | `double` | The modifier value. |

Used on: `BuildingFCDef`, `FCEventDef`, `FCPolicyDef`, `WorldSettlementDef`.

---

### BuildingFCDef

**Class**: `FactionColonies.BuildingFCDef` (extends `Def`)

Defines a building that can be constructed in settlements. Buildings provide stat bonuses and can form upgrade chains.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `desc` | `string` | — | Description shown in the building info panel. |
| `cost` | `double` | — | Silver cost to construct. |
| `constructionDuration` | `int` | — | Build time in ticks (60000 = 1 day). |
| `techLevel` | `TechLevel` | `Undefined` | Minimum tech level. |
| `statModifiers` | `List<FCStatModifier>` | `[]` | Stat bonuses while built. |
| `applicableBiomes` | `List<string>` | `[]` | BiomeResourceDef defNames where this building is available. Empty = all biomes. |
| `upkeep` | `int` | `0` | Silver upkeep cost per period. |
| `iconPath` | `string` | `"GUI/unrest"` | Path to icon texture. |
| `settlementTypeAllowList` | `List<WorldSettlementDef>` | `[]` | Settlement types where this can be built. Mutually exclusive with block list. |
| `settlementTypeBlockList` | `List<WorldSettlementDef>` | `[]` | Settlement types where this cannot be built. |
| `minhilliness` | `Hilliness` | `Undefined` | Minimum tile hilliness. |
| `maxhilliness` | `Hilliness` | `Undefined` | Maximum tile hilliness. |
| `baseBuilding` | `bool` | `true` | If false, only obtainable via upgrade (not directly buildable). |
| `upgrades` | `List<BuildingFCDef>` | `[]` | Buildings this can be upgraded into. |
| `requiredBuildings` | `List<BuildingFCDef>` | `[]` | Prerequisites that must be built first. |

**Supported modExtensions**: [BuildingFCExtension](def-mod-extensions.md#buildingfcextension) (for custom C# building comps).

See [ExampleDefs/BuildingFCDef.xml](ExampleDefs/BuildingFCDef.xml).

---

### BiomeResourceDef

**Class**: `FactionColonies.BiomeResourceDef` (extends `Def`)

Defines per-biome resource availability and base production values.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `canSettle` | `bool` | — | Whether players can create settlements in this biome. |
| `descriptionKey` | `string` | — | Translation key for biome description. Falls back to `"FCDescUnknown"`. |
| `resources` | `List<ResourceAvailability>` | `[]` | Per-resource `{resourceDef, additive, multiplier}` values. |
| `resourceBlockList` | `List<ResourceTypeDef>` | `[]` | Resources completely unavailable in this biome (overrides everything). |

See [ExampleDefs/BiomeResourceDef.xml](ExampleDefs/BiomeResourceDef.xml).

---

### FCEventDef

**Class**: `FactionColonies.FCEventDef` (extends `Def`)

Defines events — both scripted (triggered by code) and random (selected by the random event system). See [Event System](event-system.md) for the full lifecycle.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `timeTillTrigger` | `int` | `-1` | Ticks until resolution. -1 = immediate. |
| `desc` | `string` | — | Description shown in the event notification. |
| `category` | `FCEventCategoryDef` | — | Event category for UI accent color. |
| `isRandomEvent` | `bool` | `false` | Eligible for random selection. |
| `weight` | `int` | `0` | Random selection weight (higher = more likely). |
| `requiredWealth` | `int` | `0` | Minimum player wealth for random events. |
| `rangeSettlementsAffected` | `IntRange` | `(0,0)` | How many settlements affected. (0,0) = faction-wide. |
| `requiredResource` | `ResourceTypeDef` | `null` | At least one settlement must produce this resource. |
| `applicableBiomes` | `List<string>` | `[]` | Biome allowlist (BiomeDef defNames). Only settlements in listed biomes are eligible. Empty = all. |
| `restrictedBiomes` | `List<string>` | `[]` | Biome blocklist (BiomeDef defNames). Settlements in listed biomes are excluded. Ignored if `applicableBiomes` is set. |
| `options` | `List<FCOptionDef>` | `[]` | Player choices for this event. |
| `eventFollows` | `bool` | `false` | Whether a follow-up event fires on resolution. |
| `followingEvent` | `FCEventDef` | `null` | The follow-up event. |
| `loot` | `List<ThingDef>` | `[]` | Specific items given on resolution. |
| `randomThingValue` | `int` | `0` | Base market value for random rewards. |
| `statModifiers` | `List<FCStatModifier>` | `[]` | Stats applied while event is active. |
| `useProximity` | `bool` | `true` | If true, random settlement selection uses proximity-based weighting. |
| `proximityFalloff` | `float` | `20` | Falloff distance for proximity weighting. Higher = distance matters less. |
| `isMilitaryEvent` | `bool` | `false` | Whether this is a combat event. |

See [ExampleDefs/FCEventDef.xml](ExampleDefs/FCEventDef.xml) for all fields including random event config and event chains.

**Supported modExtensions**: [FCEventHandlerExtension](def-mod-extensions.md#fceventhandlerextension).

---

### FCEventCategoryDef

**Class**: `FactionColonies.FCEventCategoryDef` (extends `Def`)

Defines a color-coded event category for UI grouping.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `color` | `Color` | `(0.65, 0.65, 0.65, 1)` | UI accent color (RGBA, 0-1 each). |
| `displayOrder` | `int` | `100` | Sort order in filter lists. Lower = earlier. |

See [ExampleDefs/FCEventCategoryDef.xml](ExampleDefs/FCEventCategoryDef.xml).

---

### FCOptionDef

**Class**: `FactionColonies.FCOptionDef` (extends `Def`)

Defines a player choice within an event.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `baseChanceOfSuccess` | `int` | — | Success chance (0-100). UI labels: 100+ = "Guaranteed", 75-99 = "Likely", 40-74 = "Uncertain", 0-39 = "Risky". |
| `silverCost` | `int` | `0` | Silver cost to choose this option. |
| `parentEvent` | `FCEventDef` | — | The event this option belongs to. |
| `successEvent` | `FCEventDef` | `null` | Event fired on success. |
| `failEvent` | `FCEventDef` | `null` | Event fired on failure. |
| `requiredPolicies` | `List<FCPolicyDef>` | `[]` | Policies that must be active for this option to be available. |
| `requirementMode` | `FCRequirementMode` | `All` | Whether `All` or `Any` required policies must be active. |

See [ExampleDefs/FCOptionDef.xml](ExampleDefs/FCOptionDef.xml).

---

### FCPolicyDef

**Class**: `FactionColonies.FCPolicyDef` (extends `Def`)

Defines policies, traits, and edicts. The same def type serves multiple UI categories.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `desc` | `string` | — | Description. |
| `category` | `FCPolicyCategory` | — | `Core`, `Trait`, `Tax`, `Military`, `Social`, or `Undefined`. |
| `techLevelRequirement` | `TechLevel` | `Undefined` | Minimum tech level to enact. |
| `factionLevelRequirement` | `int` | `0` | Minimum settlement count to enact. |
| `cost` | `int` | `0` | Silver cost to enact. |
| `enactDuration` | `int` | `0` | Enactment time in ticks. |
| `upkeepSilver` | `int` | `0` | Periodic silver upkeep while active. |
| `statModifiers` | `List<FCStatModifier>` | `[]` | Stats modified while active. |
| `blockedActions` | `List<FCActionType>` | `[]` | Actions blocked while active. |
| `enabledActions` | `List<FCActionType>` | `[]` | Actions enabled by this policy. |
| `blockedMilitaryJobs` | `List<MilitaryJobDef>` | `[]` | Military jobs blocked while active. |
| `enabledMilitaryJobs` | `List<MilitaryJobDef>` | `[]` | Military jobs enabled by this policy. |
| `preventBuildingDestruction` | `bool` | `false` | Prevents building demolition while active. |
| `suppressMemberDeathPenalty` | `bool` | `false` | Suppresses happiness/loyalty penalty on member death. |
| `behaviorClass` | `Type` | `null` | C# class for procedural logic. Must extend [FCPolicyBehavior](abstract-base-classes.md#fcpolicybehavior). |
| `incompatiblePolicies` | `List<FCPolicyDef>` | `[]` | Mutually exclusive policies. |
| `positiveEffects` | `List<string>` | — | Translation keys for positive effect descriptions. |
| `negativeEffects` | `List<string>` | — | Translation keys for negative effect descriptions. |
| `iconPathLight` / `iconPathDark` | `string` | — | Icon paths for light/dark themes. |

**FCActionType values**: `DeployMilitary`, `SendDiplomat`, `DeployExtraSquad`, `BuildRoadsToAllies`, `UseFireSupport`, `SendPrisoner`, `SellPrisoner`, `DemolishBuilding`, `UpgradeSettlement`, `TradeWithSettlement`.

See [ExampleDefs/FCPolicyDef.xml](ExampleDefs/FCPolicyDef.xml).

---

### MilitaryJobDef

**Class**: `FactionColonies.MilitaryJobDef` (extends `Def`)

Defines a military operation type. Requires a C# handler class.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `handlerClass` | `Type` | — | **Required.** C# class extending [MilitaryJobHandler](abstract-base-classes.md#militaryjobhandler). |
| `statusLabelKey` | `string` | — | Translation key for status label (e.g., "Raiding"). |
| `floatMenuLabelKey` | `string` | — | Translation key for float menu label. |
| `floatMenuDescKey` | `string` | — | Translation key for float menu tooltip. |
| `occupiesTarget` | `bool` | `true` | If true, the target location is "occupied" during this job. |
| `isState` | `bool` | `false` | If true, represents a persistent state rather than a one-time action. |
| `cooldownStatDef` | `FCStatDef` | — | Stat that modifies cooldown duration. |
| `deadPawnCooldown` | `bool` | `false` | If true, uses the dead pawn cooldown formula. |
| `defaultEnabled` | `bool` | `true` | If false, must be explicitly enabled by an `FCPolicyDef.enabledMilitaryJobs`. |

See [ExampleDefs/MilitaryJobDef.xml](ExampleDefs/MilitaryJobDef.xml).

---

### ResourceEventRewardDef

**Class**: `FactionColonies.ResourceEventRewardDef` (extends `Def`)

Controls how random reward items are generated for events. Referenced by `FCEventDef.randomThingRewardDef`.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `thingCategoryAllowList` | `List<ThingCategoryDef>` | `[]` | Thing categories to include. |
| `thingAllowList` | `List<ThingDef>` | `[]` | Individual Things to include. |
| `thingBlockList` | `List<ThingDef>` | `[]` | Things to exclude. |
| `stuffCategoryAllowList` | `List<StuffCategoryDef>` | `[]` | Stuff categories to include. |
| `stuffCategoryBlockList` | `List<StuffCategoryDef>` | `[]` | Stuff categories to exclude. |
| `useValueFactors` | `bool` | `false` | If true, use factor mode for value range. If false, use flat offset mode. |
| `valueMinFlatOffset` / `valueMaxFlatOffset` | `int` | `-300` / `300` | Flat offset mode: `range = (base + min, base + max)`. |
| `valueMinFactor` / `valueMaxFactor` | `float` | `0.5` / `2.0` | Factor mode: `range = (base * min, base * max)`. |
| `useCountRange` | `bool` | `true` | Whether to apply count range. |
| `countRange` | `IntRange` | `(1, 1)` | Item count range. |
| `useQualityGenerator` | `bool` | `false` | Whether to apply quality distribution. |
| `qualityGenerator` | `QualityGenerator` | `Gift` | Quality distribution (e.g., `Gift`, `Reward`, `BaseGen`). |
| `thingSetMakerClass` | `Type` | `null` | Custom ThingSetMaker. Default: `ThingSetMaker_MarketValue`. |
| `overrideTechLevel` | `bool` | `false` | If true, uses `techLevel` below instead of faction tech level. |
| `techLevel` | `TechLevel` | `Undefined` | Tech level override. |

See [ExampleDefs/ResourceEventRewardDef.xml](ExampleDefs/ResourceEventRewardDef.xml).
