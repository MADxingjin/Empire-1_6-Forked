# Empire Submod Documentation

Empire exposes a layered extensibility system designed for submods to add content without Harmony patches in most cases. This documentation covers every extension point available.

---

## Documentation Index

| Guide | What it covers |
|-------|---------------|
| [Getting Started](getting-started.md) | Empire-specific conventions, access points, registration patterns |
| [XML Def Types](xml-defs.md) | All 12 custom def types — fields, defaults, cross-references |
| [Stat & Production System](stat-system.md) | FCStatDef, aggregation pipeline, resource production formula |
| [Interfaces & Registries](interfaces-and-registries.md) | 12 C# interfaces, 9 static registries — method signatures, invocation timing |
| [DefModExtensions](def-mod-extensions.md) | 8 DefModExtension classes for events, buildings, resources, settlements |
| [Abstract Base Classes](abstract-base-classes.md) | FCPolicyBehavior, SettlementBuildingComp, MilitaryJobHandler |
| [Settlement Comps](worldobject-comps.md) | WorldObjectComp pattern for per-settlement extensibility in Empire |
| [Event System](event-system.md) | Event lifecycle, chains, options, handler extensions |

Annotated XML examples for every def type are in [ExampleDefs/](ExampleDefs/).

---

## What do you want to do?

| Goal | Start here |
|------|-----------|
| Add new buildings, events, resources, or policies (XML only) | [XML Def Types](xml-defs.md) |
| Define a new resource type | [XML Def Types — ResourceTypeDef](xml-defs.md#resourcetypedef) + [Stat System](stat-system.md) |
| Define a new settlement type | [XML Def Types — WorldSettlementDef](xml-defs.md#worldsettlementdef) + [DefModExtensions — SettlementTypeExtension](def-mod-extensions.md#settlementtypeextension) |
| Add a tab to the settlement window | [Settlement Comps](worldobject-comps.md) (ISettlementWindowOverview) |
| Add a tab to the main Empire window | [Interfaces & Registries](interfaces-and-registries.md#imaintabwindowoverview) |
| Modify resource production dynamically | [Settlement Comps](worldobject-comps.md) (IResourceProductionModifier) + [Stat System](stat-system.md) |
| Modify stats dynamically | [Settlement Comps](worldobject-comps.md) (IStatModifierProvider) + [Stat System](stat-system.md) |
| Hook into settlement/building/military lifecycle | [Interfaces & Registries](interfaces-and-registries.md#ilifecycleparticipant) |
| Intercept or modify tax collection | [Interfaces & Registries](interfaces-and-registries.md#itaxtickparticipant) |
| Modify battle outcomes | [Interfaces & Registries](interfaces-and-registries.md#ibattlemodifier) |
| Restrict defense assignments or squad assignments | [Interfaces & Registries](interfaces-and-registries.md#idefensevalidator) |
| Modify Empire Threat Level | [Interfaces & Registries](interfaces-and-registries.md#ithreatscalingcontributor) |
| Intercept silver payments | [Interfaces & Registries](interfaces-and-registries.md#isilverpaymentmodifier) |
| Create a custom policy with procedural logic | [Abstract Base Classes — FCPolicyBehavior](abstract-base-classes.md#fcpolicybehavior) |
| Create a building with custom C# behavior | [Abstract Base Classes — SettlementBuildingComp](abstract-base-classes.md#settlementbuildingcomp) |
| Create a custom military operation | [Abstract Base Classes — MilitaryJobHandler](abstract-base-classes.md#militaryjobhandler) |
| Add custom event resolution logic | [DefModExtensions — FCEventHandlerExtension](def-mod-extensions.md#fceventhandlerextension) + [Event System](event-system.md) |
| Add a custom resource filter, production extension, or pool | [DefModExtensions — Resource Extensions](def-mod-extensions.md#resourcefilterextension) |
| Add building filter buttons to the building UI | [Interfaces & Registries](interfaces-and-registries.md#buildingfilterregistry) |
