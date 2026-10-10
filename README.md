# I Have Too Much Item Bloat

Source for the [I Have Too Much Item Bloat](IHaveTooMuchItemBloat/Thunderstore/README.md) Risk of Rain 2 mod.

## Building

Requires the .NET SDK (6 or later). Game, BepInEx, R2API and Risk Of Options references come from NuGet (`NuGet.config` adds the BepInEx feed).

```
dotnet build IHaveTooMuchItemBloat.sln
```

Every build copies the mod into the r2modman profile. A Release build (`-c Release`) also writes a Thunderstore package to `dist/`.

The default profile is `Main`. To use another, create `Directory.Build.props.user` next to the solution:

```xml
<Project>
  <PropertyGroup>
    <ProfileName>Default</ProfileName>
  </PropertyGroup>
</Project>
```
