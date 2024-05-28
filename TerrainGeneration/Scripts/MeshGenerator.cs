using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail)
    {
        int skipIncrement = (levelOfDetail == 0)?1:levelOfDetail * 2;
        int numberOfVerticesPerLine = meshSettings.numberOfVerticesPerLine;

        Vector2 topLeft = new Vector2(-1, 1) * meshSettings.meshWorldSize / 2f;

        MeshData meshData = new MeshData(numberOfVerticesPerLine,skipIncrement, meshSettings.useFlatShading);

        int[,] vertexIndicesMap = new int[numberOfVerticesPerLine, numberOfVerticesPerLine];
        int meshVertexIndex = 0;
        int outsideOfVertexIndex = -1;

        for (int y = 0; y < numberOfVerticesPerLine; y++)
        {
            for (int x = 0; x < numberOfVerticesPerLine; x++)
            {
                bool isOutsideOfMeshVertex = y == 0 || y == numberOfVerticesPerLine -1 || x == 0 || x == numberOfVerticesPerLine - 1;
                bool isSkippedVertex = x > 2 && x < numberOfVerticesPerLine - 3 && y > 2 && y < numberOfVerticesPerLine - 3 && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);
                if (isOutsideOfMeshVertex)
                {
                    vertexIndicesMap[x, y] = outsideOfVertexIndex;
                    outsideOfVertexIndex--; 
                }else if (!isSkippedVertex)
                {
                    vertexIndicesMap[x, y ] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y < numberOfVerticesPerLine; y++)
        {
            for (int x = 0; x < numberOfVerticesPerLine; x++)
            {
                bool isSkippedVertex = x > 2 && x < numberOfVerticesPerLine-3 && y > 2 && y < numberOfVerticesPerLine-3 && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);
                
                if (!isSkippedVertex)
                {
                    bool isOutsideOfMeshVertex = y==0 || y==numberOfVerticesPerLine-1 || x==0 || x==numberOfVerticesPerLine-1;
                    bool isMeshEdgeVertex = y==1 || y==numberOfVerticesPerLine-2 || x==1 || x==numberOfVerticesPerLine-2 && !isOutsideOfMeshVertex;
                    bool isMainVertex = (x-2) % skipIncrement == 0 && (y-2) % skipIncrement == 0 && !isOutsideOfMeshVertex && !isMeshEdgeVertex;
                    bool isEdgeConnectionVertex = (y==2 || y==numberOfVerticesPerLine-3 || x==2 || x==numberOfVerticesPerLine-3) && !isOutsideOfMeshVertex && !isMeshEdgeVertex && !isMainVertex;

                    int vertexIndex = vertexIndicesMap[x, y];

                    Vector2 percent = new Vector2(x-1, y-1) / (numberOfVerticesPerLine-3);
                    Vector2 vertexPosition2D = topLeft + new Vector2(percent.x, -percent.y) * meshSettings.meshWorldSize;

                    float height = heightMap[x, y];
                    
                    //old scale
                    //Vector3 vertexPosition = new Vector3((topLeftX + percent.x * meshSizeUnsimplified) * meshSettings.meshScale, height, (topLeftZ - percent.y * meshSizeUnsimplified) * meshSettings.meshScale);
                    if (isEdgeConnectionVertex)
                    {
                        bool isVertical = x==2 || x==numberOfVerticesPerLine - 3; 
                        int distanceToMainVertexA = ((isVertical)? y-2 : x-2) % skipIncrement;
                        int distanceToMainVertexB = skipIncrement - distanceToMainVertexA;
                        float distancePercentFromAtoB = distanceToMainVertexA / (float)skipIncrement;

                        float heightOfMainVertexA = heightMap[(isVertical) ? x : x - distanceToMainVertexA, (isVertical) ? y - distanceToMainVertexA : y];
                        float heightOfMainVertexB = heightMap[(isVertical) ? x : x + distanceToMainVertexB, (isVertical) ? y + distanceToMainVertexB : y];

                        height = heightOfMainVertexA * (1 - distancePercentFromAtoB) + heightOfMainVertexB * distancePercentFromAtoB;
                    }

                    meshData.AddVertex(new Vector3 (vertexPosition2D.x, height, vertexPosition2D.y) , percent, vertexIndex);

                    bool isCreateTriangle = x < numberOfVerticesPerLine-1 && y < numberOfVerticesPerLine-1 && (!isEdgeConnectionVertex || (x!=2 && y!=2));

                    if (isCreateTriangle)
                    {
                        int currentIncrement = (isMainVertex && x != numberOfVerticesPerLine-3 && y != numberOfVerticesPerLine-3)? skipIncrement:1;

                        int a = vertexIndicesMap[x, y];
                        int b = vertexIndicesMap[x + currentIncrement, y];
                        int c = vertexIndicesMap[x, y + currentIncrement];
                        int d = vertexIndicesMap[x + currentIncrement, y + currentIncrement];

                        meshData.AddTriangle(a, d, c);
                        meshData.AddTriangle(d, a, b);
                    }
                }
            }
        }

        meshData.FinalizeMesh();

        return meshData;
    }
}


public class MeshData
{
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;
    Vector3[] bakedNormals;

    Vector3[] outsideOfMeshVertices;
    int[] outsideOfMeshTriangles;

    int triangleIndex;
    int outsideOfMeshTriangleIndex;

    bool useFlatShading;

    public MeshData(int numberOfVerticesPerLine, int skipIncrement, bool useFlatShading)
    {
        this.useFlatShading = useFlatShading;

        int numberOfMeshEdgeVertices = (numberOfVerticesPerLine - 2) * 4 - 4;
        int numberOfEdgeConnectionVertices = (skipIncrement - 1) * (numberOfVerticesPerLine - 5) / skipIncrement * 4;
        int numberOfMainVerticesPerLine = (numberOfVerticesPerLine - 5) / skipIncrement + 1;
        int numberOfMainVertices = numberOfMainVerticesPerLine * numberOfMainVerticesPerLine;

        vertices = new Vector3[numberOfMeshEdgeVertices + numberOfEdgeConnectionVertices + numberOfMainVertices];
        uvs = new Vector2[vertices.Length];

        int numberOfMeshEdgeTriangles = 8 * (numberOfVerticesPerLine - 4); 
        int numberOfMainTriangles = (numberOfMainVerticesPerLine - 1) *(numberOfMainVerticesPerLine - 1) * 2;
        triangles = new int[(numberOfMeshEdgeTriangles + numberOfMainTriangles) * 3];

        outsideOfMeshVertices = new Vector3[numberOfVerticesPerLine * 4 - 4];
        outsideOfMeshTriangles = new int[24 * numberOfVerticesPerLine - 2];
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        if (vertexIndex < 0)
        {
            outsideOfMeshVertices[-vertexIndex - 1] = vertexPosition;
        }else
        {
            vertices[vertexIndex] = vertexPosition;
            uvs[vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            outsideOfMeshTriangles[outsideOfMeshTriangleIndex] = a;
            outsideOfMeshTriangles[outsideOfMeshTriangleIndex + 1] = b;
            outsideOfMeshTriangles[outsideOfMeshTriangleIndex + 2] = c;
            outsideOfMeshTriangleIndex += 3;
        }else
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }
    }

    Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length / 3;

        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = outsideOfMeshTriangles.Length / 3;

        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = outsideOfMeshTriangles[normalTriangleIndex];
            int vertexIndexB = outsideOfMeshTriangles[normalTriangleIndex + 1];
            int vertexIndexC = outsideOfMeshTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            if (vertexIndexA >= 0)
            {
                vertexNormals[vertexIndexA] += triangleNormal;
            }
            if (vertexIndexB >= 0)
            {
                vertexNormals[vertexIndexB] += triangleNormal;
            }
            if (vertexIndexC >= 0)
            {
                vertexNormals[vertexIndexC] += triangleNormal;
            }
        }
        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }
        return vertexNormals;
    }

    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = (indexA < 0)?outsideOfMeshVertices[-indexA-1] : vertices[indexA];
        Vector3 pointB = (indexB < 0)?outsideOfMeshVertices[-indexB-1] : vertices[indexB];
        Vector3 pointC = (indexC < 0)?outsideOfMeshVertices[-indexC-1] : vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 SideAC = pointC - pointA;
        return Vector3.Cross(sideAB, SideAC).normalized;
    }
    public void FinalizeMesh()
    {
        if (useFlatShading)
        {
            FlatShading();
        }else
        {
            BakeNormals();
        }
    }

    private void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }

    void FlatShading()
    {
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUVs = new Vector2[triangles.Length];

        for (int i = 0; i < triangles.Length; i++)
        {
            flatShadedVertices[i] = vertices[triangles[i]];
            flatShadedUVs[i] = uvs[triangles[i]];

            triangles[i] = i;
        }

        vertices = flatShadedVertices;
        uvs = flatShadedUVs;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        if(useFlatShading)
        {
            mesh.RecalculateNormals();
        }else
        {
            mesh.normals = bakedNormals;
        }

        return mesh;
    }
}
