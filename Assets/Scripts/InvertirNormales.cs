using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class InvertirNormales : MonoBehaviour
{
    void Start()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] normales = mesh.normals;

        for (int i = 0; i < normales.Length; i++)
        {
            normales[i] = -normales[i];
        }

        mesh.normals = normales;

        int[] triangulos = mesh.triangles;
        for (int i = 0; i < triangulos.Length; i += 3)
        {
            int temp = triangulos[i];
            triangulos[i] = triangulos[i + 2];
            triangulos[i + 2] = temp;
        }

        mesh.triangles = triangulos;
    }
}