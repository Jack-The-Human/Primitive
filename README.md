# Primitive
Primitives

Re-attach scripts or recreate the 3 items in TerrainAssets and ensure all values are filled or selected;
-mesh settings
-terrain data
-height map settings

create new gameobject name of "Map Generator" attach Map Preview script
-Add "Plane" gameobject to the Map Generator object name of "Preview Plane"
-Add empty gameobject to the Map Generator object name of "Preview Mesh"
---Add a Mesh Filter to Preview Mesh
---Add a Mesh Renderer to Preview Mesh and attach the MeshMaterial - if pink, ensure the meshmaterial shader is set to the Terrain shader
-ensure Map preview has everything attached including the MeshMaterial

Map Preview should be functioning at this point if you select Generate;
