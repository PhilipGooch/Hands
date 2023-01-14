using NBG.MeshGeneration;
using System.IO;
using Unity.Mathematics;
using UnityEngine;

namespace NBG.PlaneDestructionSystem.Tests
{
    public static class DestructionTestsCodegen
    {
        public static void GeneratePolygonCode(Polygon2D polygon, string name)
        {
            File.WriteAllText(GetPolygonPath(name), GeneratePolygonCodeString(polygon, name));
        }

        public static string GetPolygonPath(string name)
        {
            return Application.dataPath + "/Modules/PlaneDestructionSystem/Tests.Editor/GeneratedPolygonTemplates/" + name + ".cs";
        }

        private static string GeneratePolygonCodeString(Polygon2D p, string name)
        {
            string code = "";

            string i = "";
            code += "using Unity.Mathematics;\n";
            code += "namespace NBG.PlaneDestructionSystem.Codegen\n";
            code += "{\n";

            i = "\t";

            code += i + "internal static class " + name + "\n";
            code += i + "{\n";

            i = "\t\t";

            code += i + "public static Polygon2D polygon => new Polygon2D(outline, holes);\n";
            code += i + "public static float3[] vertices => _vertices;\n";
            code += i + "public static int[] triangles => _triangles;\n\n";

            code += Print1DFloat3Array(p.vertices.ToArray(), "outline", 2);

            code += "\n";

            code += i + "public static float3[][] holes =\n";
            code += i + "{\n";


            for (int j = 0; j < p.holes.Count; j++)
            {
                i = "\t\t\t";
                code += i + "new float3[] {\n";
                var hole = p.holes[j];

                i = "\t\t\t\t";
                for (int x = 0; x < hole.Count; x++)
                {
                    code += i + "new float3(";

                    var pos = hole[x];
                    code += pos.x + "f, " + pos.y + "f, " + pos.z + "f";

                    code += ")";

                    if (x < (hole.Count - 1))
                        code += ", ";

                    code += "\n";
                }


                i = "\t\t\t";
                code += i + "}";
                if (j < (p.holes.Count - 1))
                    code += ",\n\n";
                else
                    code += "\n";
            }
            i = "\t\t";
            code += i + "};\n";

            code += "\n";

            #region Vertices
            var meshVertices = p.extrudedPolygonVertices;
            code += i + "public static float3[] _vertices =\n";
            code += i + "{\n";

            i = "\t\t\t";

            for (int x = 0; x < meshVertices.Length; x++)
            {
                code += i + "new float3(";

                var pos = meshVertices[x];
                code += pos.x + "f, " + pos.y + "f, " + pos.z + "f";

                code += ")";

                if (x < (meshVertices.Length - 1))
                    code += ", ";
                code += "\n";
            }

            i = "\t\t";
            code += i + "};\n";

            #endregion

            code += "\n";

            #region Triangles
            var meshTriangles = p.extrudedTriangles;
            code += i + "public static int[] _triangles =\n";
            code += i + "{\n";

            i = "\t\t\t";

            for (int x = 0; x < meshTriangles.Length; x++)
            {
                code += i + meshTriangles[x];
                if (x < (meshTriangles.Length - 1))
                    code += ", ";
                code += "\n";
            }

            i = "\t\t";
            code += i + "};\n";

            #endregion

            i = "\t";
            code += i + "}\n";
            code += "}";
            return code;
        }


        public static string Print1DFloat3Array(float3[] array, string varName, int tabCount)
        {
            string tabs = Tabs(tabCount);
            string code = "";

            code += tabs + "public static float3[] " + varName + "  =\n";
            code += tabs + "{\n";

            tabs = Tabs(++tabCount);

            for (int x = 0; x < array.Length; x++)
            {
                code += tabs + "new float3(";

                var pos = array[x];
                code += pos.x + "f, " + pos.y + "f, " + pos.z + "f";

                code += ")";

                if (x < (array.Length - 1))
                    code += ", ";
                code += "\n";
            }

            tabs = Tabs(++tabCount);

            code += tabs + "};\n";

            return code;
        }

        public static string Tabs(int count)
        {
            string tabs = "";
            for (int i = 0; i < count; i++)
                tabs += "\t";

            return tabs;
        }

        public static void GenerateBreakCode(Polygon2D polygon, float3 pos, float shatter1, float shatter2)
        {
            string name = GetBreakName();
            File.WriteAllText(GetBreakPath(name), GenerateBreakCodeString(name, polygon, pos, shatter1, shatter2));
        }

        private static string GetBreakName()
        {
            string prefix = "BreakCase";
            int number = 0;

            string path = Application.dataPath + "/Modules/PlaneDestructionSystem/Tests/GeneratedBreakTests/";
            while (File.Exists(path + prefix + number + ".cs"))
                number++;
            return prefix + number;
        }

        private static string GetBreakPath(string name)
        {
            return Application.dataPath + "/Modules/PlaneDestructionSystem/Tests/GeneratedBreakTests/" + name + ".cs";
        }

        private static string GenerateBreakCodeString(string name, Polygon2D shape, float3 pos, float shatter1, float shatter2)
        {
            string code = "";

            string i = "";
            code += "using Unity.Mathematics;\n";
            code += "namespace NBG.PlaneDestructionSystem.Codegen\n";
            code += "{\n";

            i = "\t";

            code += i + "internal class " + name + " : IBreakTest\n";
            code += i + "{\n";

            i = "\t\t";

            code += i + "public static float shatter1 = " + shatter1 + "f;\n";
            code += i + "public static float shatter2 = " + shatter2 + "f;\n";
            code += i + "public static float3 pos = new float3(" + pos.x + "f, " + pos.y + "f, " + pos.z + "f);\n";

            code += Print1DFloat3Array(shape.vertices.ToArray(), "shape", 2);

            code += "\n";



            i = "\t";
            code += i + "}\n";
            code += "}";
            return code;
        }
    }
}
