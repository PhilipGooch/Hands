using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.IO;
using System.Linq;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace NBG.Core.CodeGen
{
    public static class PostProcessorUtility
    {
        /*class CompiledAssembly : ICompiledAssembly
        {
            public InMemoryAssembly InMemoryAssembly { get; set; }
            public string Name { get; set; }
            public string[] References { get; set; }
            public string[] Defines { get; set; }
        }

        internal static void Process(string name, byte[] peData, byte[] pdbData, string[] defines, string[] references)
        {
            var compiledAssembly = new CompiledAssembly
            {
                Name = name,
                InMemoryAssembly = new InMemoryAssembly(peData, pdbData),
                Defines = defines,
                References = references
            };
        
            new PostProcessor().Process(compiledAssembly);
        }*/

        public static ILPostProcessResult CreatePostProcessResult(AssemblyDefinition assembly)
        {
            using (var pe = new MemoryStream())
            using (var pdb = new MemoryStream())
            {
                var writerParameters = new WriterParameters
                {
                    WriteSymbols = true,
                    SymbolStream = pdb,
                    SymbolWriterProvider = new PortablePdbWriterProvider()
                };

                assembly.Write(pe, writerParameters);
                return new ILPostProcessResult(new InMemoryAssembly(pe.ToArray(), pdb.ToArray()));
            }
        }

        public static AssemblyDefinition CreateAssemblyDefinition(ICompiledAssembly compiledAssembly)
        {
            var resolver = new PostProcessorAssemblyResolver(compiledAssembly);

            AssemblyDefinition assemblyDefinition = null;

            // BUG: In some cases the assembly resolver fails to load symbol information. In this case we retry without symbols.
            try
            {
                var readerParameters = new ReaderParameters
                {
                    AssemblyResolver = resolver,
                    ReadingMode = ReadingMode.Deferred,

                    // We _could_ be running in .NET core. In this case we need to force imports to resolve to mscorlib.
                    ReflectionImporterProvider = new PostProcessorReflectionImporterProvider()
                };

                if (null != compiledAssembly.InMemoryAssembly.PdbData)
                {
                    readerParameters.ReadSymbols = true;
                    readerParameters.SymbolStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData.ToArray());
                    readerParameters.SymbolReaderProvider = new PortablePdbReaderProvider();
                }
                else
                {
                    System.Console.WriteLine("[NBG.Core.CodeGen] Will not read symbols.");
                }

                var peStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PeData.ToArray());
                assemblyDefinition = AssemblyDefinition.ReadAssembly(peStream, readerParameters);
                resolver.AddAssemblyDefinitionBeingOperatedOn(assemblyDefinition);
            }
            catch (BadImageFormatException)
            {
                System.Console.WriteLine("[NBG.Core.CodeGen] BadImageFormatException.");

                var readerParameters = new ReaderParameters
                {
                    AssemblyResolver = resolver,
                    ReadingMode = ReadingMode.Deferred,

                    // We _could_ be running in .NET core. In this case we need to force imports to resolve to mscorlib.
                    ReflectionImporterProvider = new PostProcessorReflectionImporterProvider()
                };

                var peStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PeData.ToArray());
                assemblyDefinition = AssemblyDefinition.ReadAssembly(peStream, readerParameters);
                resolver.AddAssemblyDefinitionBeingOperatedOn(assemblyDefinition);
            }

            return assemblyDefinition;
        }
    }
}
