# Coastal Trees

Two realistic coastal trees selected to match First Island's existing Island Tree 01.

## Source and license

- Island Tree 02: https://polyhaven.com/a/island_tree_02
- Island Tree 03: https://polyhaven.com/a/island_tree_03
- Authors: Rico Cilliers (cleanup and processing), Rob Tuytel (scanning and processing).
- License: CC0 1.0 Universal — https://creativecommons.org/publicdomain/zero/1.0/
- Original FBX models and textures downloaded from Poly Haven on 2026-10-01.
- Download URLs, sizes and official MD5 hashes are recorded in SourceFiles.json.
- Powered by Poly Haven: https://polyhaven.com/ (download metadata from its public API).

## Unity use

Drag IslandTree02 or IslandTree03 from Prefabs into the Environment scene.
Materials use the project's URP and Mavis foliage shaders. Leaf cutouts and shared
branch/leaf textures are included. The prefab LODGroup keeps only one detail level
visible at a time. The original model files remain available under Models.

Foliage animation uses the existing Mavis.FoliageWindDriver in the island scene.
Do not add one wind driver per tree.

The FBX axis conversion is baked into the meshes; the prefab root uses unit scale.
The base rests at local Y=0. Tree 03 includes the original scanned root/ground base,
which can be lowered slightly into the terrain when placing it.

Near / middle / far triangle counts:
- IslandTree02: 369,506 / 122,707 / 35,141 (original: 1,072,213).
- IslandTree03: 697,425 / 234,358 / 63,628 (original: 2,085,320).

These are detailed scanned assets: use the supplied LOD prefabs when placing trees.
The imported normal and roughness maps are retained. Bark uses its normal map;
leaves and branches use the existing project's foliage shader and cutout workflow.

The editor authoring utility uses UnityMeshSimplifier v3.1.1 by Mattias Edlund,
under the MIT license. Source and license are included in Editor/UnityMeshSimplifier.
