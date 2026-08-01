# Grid Heat API

`GridHeatApi.cs` exposes the fusion heat system as a small grid-level API other mods can call without knowing anything about fusion assemblies.

## Quick Use

Copy `Data/Scripts/ModularAssemblies/HeatParts/GridHeatApi.cs` into your mod and keep one instance on your session component:

```csharp
private readonly GridHeatApi _heat = new GridHeatApi();

public override void LoadData()
{
    _heat.Load();
}

protected override void UnloadData()
{
    _heat.Unload();
}
```

## Common Calls

```csharp
_heat.SetHeatCapacity(grid, 1000f);
_heat.SetHeatDissipation(grid, 20f);
_heat.AddHeat(grid, 75f);

float heat = _heat.GetHeat(grid);
float heatRatio = _heat.GetHeatRatio(grid);
```

`SetHeatCapacity` and `SetHeatDissipation` add API-owned capacity/dissipation on top of the heat blocks already provided by Fusion Systems. `AddHeat` and `SetHeat` are clamped to the grid's total heat capacity.

## HUD Position

The HUD heat bar uses the same camera-space offset as the Fusion Systems HUD. Other mods can hide or move it:

```csharp
_heat.SetHudVisible(true);
_heat.SetHudOffset(-0.76f, -0.80f, 0f);
```

The offset values are camera-space X/Y/Z values. The default is the existing Fusion Systems position.
