using Mono.Cecil;
using NBG.Core.CodeGen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace NBG.LogicGraph.CodeGen
{
    public class UserlandBindingsPostProcessor : ILPostProcessor
    {
        // Bindings will be generated for all assemblies that reference this assembly:
        const string LogicGraphRuntimeAssemblyName = "NBG.LogicGraph.Foundation";

        static readonly string[] s_ExcludeIfAssemblyNameContains = {
            "UnityEngine",
            "UnityEditor",
        };

        public override ILPostProcessor GetInstance()
        {
            return this;
        }

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            var name = compiledAssembly.Name;

            // Exclude based on name.
            if (s_ExcludeIfAssemblyNameContains.Any(x => name.Contains(x)))
                return false;

            //var isEditor = compiledAssembly.Defines.Contains("UNITY_EDITOR");
            var relevant = compiledAssembly.References.Any(filePath => Path.GetFileNameWithoutExtension(filePath) == LogicGraphRuntimeAssemblyName);
            return relevant;
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            if (!WillProcess(compiledAssembly))
                return null;

            ILPostProcessResult result = null;

            using (var assemblyDefinition = PostProcessorUtility.CreateAssemblyDefinition(compiledAssembly))
            {
                if (Generate(assemblyDefinition, compiledAssembly.Defines))
                {
                    result = PostProcessorUtility.CreatePostProcessResult(assemblyDefinition);
                }
            }

            return result;
        }

        static bool Generate(AssemblyDefinition assembly, IEnumerable<string> defines)
        {
            var gen = new UserlandBindingsGenerator(assembly, defines);
            return gen.Inject();
        }
    }
}
