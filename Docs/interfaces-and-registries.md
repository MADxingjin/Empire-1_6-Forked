# Interfaces & Registries

Empire provides 12 C# interfaces for submod extensibility. Some use static registries (global hooks); others are discovered on WorldObjectComps (per-settlement hooks).

All registry-based interfaces follow the same pattern — register an instance, and the base mod invokes it at the appropriate time.

Registries are not serialized. Your mod must re-register on game load.

---

## Registry-Based Interfaces

### ILifecycleParticipant

**Registry**: `LifecycleRegistry`
**Purpose**: Unified hook for settlement, building, military, and research events.

This is the most commonly used registry. It fires after every major state change, with caches already invalidated.

```csharp
public interface ILifecycleParticipant
{
    void OnSettlementCreated(WorldSettlementFC settlement);
    void OnSettlementRemoved(WorldSettlementFC settlement);
    void OnSettlementUpgraded(WorldSettlementFC settlement, int oldLevel, int newLevel);
    void OnSettlementTypeChanged(WorldSettlementFC settlement, WorldSettlementDef oldDef, WorldSettlementDef newDef);
    void OnBuildingConstructed(WorldSettlementFC settlement, BuildingFCDef building, int slot);
    void OnBuildingDeconstructed(WorldSettlementFC settlement, BuildingFCDef building, int slot);
    void OnSquadDeployed(WorldSettlementFC settlement, MilitaryJobDef job, bool isExtraSquad);
    void OnSquadRecalled(WorldSettlementFC settlement);
    void OnBattleResolved(WorldSettlementFC settlement, MilitaryJobDef job, bool victory, BattleResult result);
    void OnResearchCompleted(ResearchProjectDef project);
}
```

**Convenience base class**: `LifecycleParticipantBase` — all methods are empty virtuals. Extend this to avoid stubbing unused methods.

```csharp
public class MyLifecycleHook : LifecycleParticipantBase
{
    public override void OnBuildingConstructed(WorldSettlementFC settlement, BuildingFCDef building, int slot)
    {
        // Your code here — caches are already dirty
    }
}

// In your mod's static constructor:
LifecycleRegistry.Register(new MyLifecycleHook());
```

**Invocation timing**: Each hook fires after the corresponding action completes. Caches are invalidated after each participant's callback, so later participants see changes made by earlier ones.

---

### ITaxTickParticipant

**Registry**: `TaxTickRegistry`
**Purpose**: Hook into the tax/tithe collection lifecycle.

```csharp
public interface ITaxTickParticipant
{
    void PreTaxResolution(FactionFC faction);
    void PostTaxResolution(FactionFC faction);
    void PreSettlementCreateTax(WorldSettlementFC settlement);
    void PostSettlementCreateTax(WorldSettlementFC settlement, ref int silverAmount, List<Thing> titheThings);
}
```

| Method | When it fires |
|--------|--------------|
| `PreTaxResolution` | Once per tax tick, before any settlement is processed. |
| `PostTaxResolution` | Once per tax tick, after all settlements are processed. |
| `PreSettlementCreateTax` | Per settlement, after `SettlementTypeExtension.PreTax`. Caches are invalidated after each participant's callback. |
| `PostSettlementCreateTax` | Per settlement, after all calculations. You can modify `silverAmount` (ref) and `titheThings`. Caches are invalidated after each participant's callback. |

---

### IBattleModifier

**Registry**: `BattleModifierRegistry`
**Purpose**: Modify military forces before battle simulation.

```csharp
public interface IBattleModifier
{
    void ModifyForce(militaryForce force, bool isAttacker);
}
```

Called twice per battle — once for the attacker force, once for the defender force. You can modify `force.militaryLevel`, `force.militaryEfficiency`, or `force.forceRemaining`.

---

### IDefenseValidator

**Registry**: `DefenseValidatorRegistry`
**Purpose**: Veto defense assignments.

```csharp
public interface IDefenseValidator
{
    bool CanDefend(WorldSettlementFC defender, WorldSettlementFC target);
}
```

Called when a settlement is considered as a defender for another settlement (both manual selection and auto-defend). Return `false` to exclude it. **Short-circuits**: if any validator returns false, the settlement cannot defend.

---

### ISquadAssignmentValidator

**Registry**: `SquadAssignmentRegistry`
**Purpose**: Veto squad assignments with reason feedback.

```csharp
public interface ISquadAssignmentValidator
{
    bool CanAssign(WorldSettlementFC settlement, MilSquadFC squad, out string reason);
}
```

Called before a squad loadout is assigned to a settlement. Return `false` with a `reason` string to show the player why. **Short-circuits** on first rejection.

---

### IThreatScalingContributor

**Registry**: `ThreatScalingRegistry`
**Purpose**: Modify the Empire Threat Level (ETL).

```csharp
public interface IThreatScalingContributor
{
    double GetAdditiveContribution(FactionFC faction);
    double GetMultiplicativeContribution(FactionFC faction);
}
```

| Method | Effect | No-op value |
|--------|--------|-------------|
| `GetAdditiveContribution` | Added to raw ETL score before multiplication | `0` |
| `GetMultiplicativeContribution` | Multiplied into final ETL result | `1.0` |

All additive contributions are summed, then all multiplicative contributions are multiplied together.

---

### ISilverPaymentModifier

**Registry**: `SilverPaymentRegistry`
**Purpose**: Intercept and modify silver payments before processing.

```csharp
public interface ISilverPaymentModifier
{
    void ModifyPayment(SilverPaymentContext context);
}
```

**SilverPaymentContext fields:**

| Field | Type | Description |
|-------|------|-------------|
| `Amount` | `int` | The payment amount. Modify this to change how much is charged. |
| `Reason` | `string` | What the payment is for (e.g., building cost, worker upkeep). |
| `Settlement` | `WorldSettlementFC` | The settlement involved (may be null for faction-level payments). |

Modifiers are called sequentially — each sees the previous modifier's changes to `Amount`.

---

### IMainTabWindowOverview

**Registry**: `MainTableRegistry`
**Purpose**: Add tabs to the main Empire faction window.

```csharp
public interface IMainTabWindowOverview
{
    void PreOpenWindow(FactionFC faction);
    void OnTabSwitch();
    void DrawOverviewTab(Rect boundingBox);
    void PostCloseWindow();
    string TabName();
}
```

| Method | When it fires |
|--------|--------------|
| `PreOpenWindow` | When the main tab window opens. |
| `OnTabSwitch` | When the user switches to your tab. |
| `DrawOverviewTab` | Every frame while your tab is active. `boundingBox` is the drawable area. |
| `PostCloseWindow` | When the main tab window closes. |
| `TabName` | Returns the tab label string. |

Tabs from this registry appear in a separate "Empire Extensions" tab window, which is only visible when at least one tab is registered (via a custom `MainButtonWorker`).

---

### BuildingFilter + BuildingFilterRegistry

**Registry**: `BuildingFilterRegistry`
**Purpose**: Add filter buttons to the building construction UI.

`BuildingFilter` is a simple class (not an interface):

```csharp
public class BuildingFilter
{
    public string label;
    public Texture2D icon;
    public Func<BuildingFCDef, bool> predicate;
}
```

```csharp
BuildingFilterRegistry.Register(new BuildingFilter
{
    label = "Military",
    icon = myMilitaryIcon,
    predicate = def => def.statModifiers.Any(m => m.stat == FCStatDefOf.militaryBaseLevel)
});
```

Filters are cleared on cache invalidation — re-register them as needed (typically in a static constructor, since the base mod re-invokes them).

---

## Comp-Based Interfaces

These interfaces are implemented on `WorldObjectComp` classes attached to `WorldSettlementFC`. They are discovered by iterating `settlement.AllComps` — no registry needed. See [Settlement Comps](worldobject-comps.md) for how to attach a comp.

### ISettlementWindowOverview

**Purpose**: Add a tab to an individual settlement's window.

```csharp
public interface ISettlementWindowOverview
{
    void PreOpenWindow(WorldSettlementFC settlement);
    void OnTabSwitch();
    void DrawOverviewTab(Rect boundingBox);
    void PostCloseWindow();
    string OverviewTabName();
}
```

Must be implemented by a `WorldObjectComp` attached to the settlement. The settlement window discovers it at open time and adds the tab.

---

### IStatModifierProvider

**Purpose**: Contribute dynamic stat values per settlement.

```csharp
public interface IStatModifierProvider
{
    double GetStatModifier(FCStatDef stat);
    string GetStatModifierDesc(FCStatDef stat);
}
```

| Method | Description | No-op return |
|--------|-------------|-------------|
| `GetStatModifier` | Returns a value that is added (Additive) or multiplied (Multiplicative) into the settlement's stat | `0` (Additive) or `1` (Multiplicative) |
| `GetStatModifierDesc` | Returns a tooltip description string for your contribution | `null` or `""` |

**Caching**: Results are cached per settlement. Caches invalidate automatically after lifecycle events. For changes outside lifecycle callbacks, call `settlement.InvalidateStatCache()`.

---

### IResourceProductionModifier

**Purpose**: Contribute dynamic resource production bonuses per settlement.

```csharp
public interface IResourceProductionModifier
{
    double GetResourceAdditiveModifier(ResourceFC resource);
    double GetResourceMultiplierModifier(ResourceFC resource);
    string GetResourceModifierDesc(ResourceFC resource);
}
```

| Method | Description | No-op return |
|--------|-------------|-------------|
| `GetResourceAdditiveModifier` | Added to the production base | `0` |
| `GetResourceMultiplierModifier` | Multiplied into the production multiplier | `1` |
| `GetResourceModifierDesc` | Tooltip description | `null` or `""` |

**Caching**: Results are lazily cached by `ResourceFC`'s dirty flags. Invalidated automatically after lifecycle events. For changes outside lifecycle callbacks, call `settlement.InvalidateResourceCaches()`.

See [Stat System — Resource Production Formula](stat-system.md#resource-production-formula) for how these values fit into the full calculation.

---

## Registry API Summary

All registries share the same API:

```csharp
// Register
MyRegistry.Register(instance);

// Unregister
MyRegistry.Unregister(instance);

// Clear all (called internally on cache invalidation for some registries)
MyRegistry.ClearAll();

// Read-only access to registered items (most registries)
IReadOnlyList<T> items = MyRegistry.Items;  // property name varies
```

| Registry | Property | Cleared on cache invalidation? |
|----------|----------|-------------------------------|
| `LifecycleRegistry` | `.Participants` | No |
| `TaxTickRegistry` | `.Taxers` | No |
| `BattleModifierRegistry` | `.Modifiers` | No |
| `DefenseValidatorRegistry` | (none) | No |
| `SquadAssignmentRegistry` | (none) | No |
| `ThreatScalingRegistry` | `.Contributors` | No |
| `SilverPaymentRegistry` | `.Modifiers` | No |
| `MainTableRegistry` | `.Tabs` | Yes |
| `BuildingFilterRegistry` | `.Filters` | Yes |
