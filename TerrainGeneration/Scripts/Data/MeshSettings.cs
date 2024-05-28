using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class MeshSettings : UpdatableData
{
    public const int numberOfSupportedLOD = 5;
    public const int numberOfSupportedChunkSizes = 10;
    public const int numberOfSupportedFlatShadedChunkSizes = 4;
    public static readonly int[] supportedChunkSizes = {24,48,72,96,120,144,168,192,216,240};

    public float meshScale = 2.5f;
    public bool useFlatShading;
    [Range (0, numberOfSupportedChunkSizes - 1)]
    public int chunkSizeIndex;
    [Range (0, numberOfSupportedFlatShadedChunkSizes - 1)]
    public int flatShadedChunkSizeIndex;

    // LOD = 0, mesh verts is accurate (has 2 extra verts that are excluded from the final mesh but used for normals)
    public int numberOfVerticesPerLine
    {
        get
        {
            return supportedChunkSizes[(useFlatShading) ? flatShadedChunkSizeIndex : chunkSizeIndex] + 5;
        }
    }

    public float meshWorldSize
    {
        get
        {
            return ((numberOfVerticesPerLine - 3) * meshScale);
        }
    }
    
    
}
