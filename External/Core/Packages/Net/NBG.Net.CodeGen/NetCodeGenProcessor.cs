using Mono.Cecil;
using NBG.Core.CodeGen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace NBG.Net.CodeGen
{
    public class NetCodeGenProcessor : ILPostProcessor
    {
        // Bindings will be generated for all assemblies that reference this assembly:
        const string NetFoundationAssemblyName = "NBG.Net.Foundation";

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

            var relevant = compiledAssembly.References.Any(filePath => Path.GetFileNameWithoutExtension(filePath) == NetFoundationAssemblyName);
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
            var gen = new NetEventBusRegistratorGenerator(assembly, defines);
            return gen.Inject();
        }
    }
}
