# Hunyuan grass and rocks for the first island

Tencent Hunyuan generated the source grass and rock models. Each original model had about 1.5 million triangles and 4096px textures. The imported assets use simplified meshes and textures. [Optimized sources](../ArtSource/Environment/Hunyuan-20261027-r2/) · [Unity assets](../Assets/Game/Prefabs/Environment/HunyuanCandidates/)

| Asset | October 27 integration |
| --- | --- |
| Grass | A 512px cutout from the generated grass was used on four-triangle crossed cards with the existing `Mavis/FoliageWind` shader. Replacing the `r12_b` and `r12_c` meshes kept the original transforms and colliders. Across 10,694 renderers, the selected mesh triangle count went from 101,593 to 42,776. |
| Rocks | The generated mesh was welded, its normals recalculated, and the ground pad removed before making the 3,133-triangle rock. Twelve rocks use the new mesh up close and retain the original mesh at distance. Their transforms and mesh colliders were kept. |

Serialized scene checks confirmed the new references and unchanged collider counts. Editor reload, gameplay and frame-time measurements were still pending at this checkpoint. Later first-island vegetation work has a separate maintenance guide. [Clean rock source](../ArtSource/Environment/Hunyuan-20261027-clean/) · [Detailed integration notes](https://github.com/cpy114514/Dark-Brine_Rot/blob/2d2a862bba490081ab4e8607dba4b35bb73bce8b/Documentation/HunyuanEnvironment-20261027.md) · [First-island maintenance](Island1EnvironmentMaintenance-20261107.md)
