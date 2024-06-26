using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk
    {
        public static float colliderGenerationDistanceThreshold = 50f;
        public event System.Action<TerrainChunk, bool> onVisibilityChanged;
        public Vector2 coord;
        GameObject meshObject;
        Vector2 sampleCenter;
        Bounds bounds;
        MeshCollider meshCollider;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        int colliderLODIndex;



        HeightMap heightMap;
        bool heightMapRecieved;
        int previosLODIndex = -1;
        bool hasSetCollider;
        float maxViewDistance;

        HeightMapSettings heightMapSettings;
        MeshSettings meshSettings;
        Transform viewer;

        public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material material)
        {
            this.coord = coord;
            this.detailLevels = detailLevels;
            this.colliderLODIndex = colliderLODIndex;
            this.heightMapSettings = heightMapSettings;
            this.meshSettings = meshSettings;
            this.viewer = viewer;

            sampleCenter = coord * meshSettings.meshWorldSize / meshSettings.meshScale;
            Vector2 position = coord * meshSettings.meshWorldSize;
            bounds = new Bounds(position, Vector2.one * meshSettings.meshWorldSize);

            /*spawns primitive planes in an array around the player at height of 0, using the V3 called positionV3
            meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane); */

            // spawns chunks 
            meshObject = new GameObject("Terrain Chunk Mesh");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;

            meshObject.transform.position = new Vector3 (position.x, 0, position.y);
            meshObject.transform.parent = parent;
            /* sets an unbaked mesh scale
            meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale; */
            
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod);
                lodMeshes[i].updateCallback += UpdateTerrainChunks;
                if (i == colliderLODIndex)
                {
                    lodMeshes[i].updateCallback += UpdateCollisionMesh;
                }
            }

            maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold * meshSettings.meshScale;


        }

        public void LoadChunk()
        {
            ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(meshSettings.numberOfVerticesPerLine, meshSettings.numberOfVerticesPerLine, heightMapSettings, sampleCenter), OnHeightMapRecieved);
        }
        void OnHeightMapRecieved(object heightMapObject)
        {
                this.heightMap = (HeightMap)heightMapObject;
                heightMapRecieved = true;

                UpdateTerrainChunks();
        }

        Vector2 viewerPosition
        {
            get
            {
                return new Vector2(viewer.position.x, viewer.position.z);
            }
        }


        public void UpdateTerrainChunks()
        {
            if (heightMapRecieved)
            {
                float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

                bool wasVisible = IsVisible();
                bool visible = viewerDistanceFromNearestEdge <= maxViewDistance;
    
                if (visible)
                {
                    int lodIndex = 0;
    
                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDistanceFromNearestEdge > detailLevels[i].visibleDistanceThreshold * meshSettings.meshScale)
                        {
                            lodIndex = i + 1;
                        }else
                        {
                            break;
                        }
                    }
    
                    if (lodIndex != previosLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            previosLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(heightMap, meshSettings);
                        }
                    }
                }
                if (wasVisible != visible)
                {
                    SetVisible(visible);

                    if (onVisibilityChanged != null)
                    {
                        onVisibilityChanged(this, visible);
                    }
                }
                
            }
        }

        public void UpdateCollisionMesh()
        {
            if(!hasSetCollider)
            {
                float sqrDistanceFromViewerToEdge = bounds.SqrDistance(viewerPosition);
                
                if (sqrDistanceFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDistanceThreshold  * meshSettings.meshScale)
                {
                    if (!lodMeshes[colliderLODIndex].hasRequestedMesh)
                    {
                        lodMeshes[colliderLODIndex].RequestMesh(heightMap, meshSettings);
                    }
                }

                if (sqrDistanceFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold)
                {
                    if (lodMeshes[colliderLODIndex].hasMesh)
                    {
                        meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
                        hasSetCollider = true;
                    }
                }
            }
            
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        public event System.Action updateCallback;

        public LODMesh(int lod)
        {
            this.lod = lod;
        }

        void OnMeshDataRecieved(object meshDataObject)
        {
            mesh = ((MeshData)meshDataObject).CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
        {
            hasRequestedMesh = true;
            ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, lod), OnMeshDataRecieved);
        }

    }
