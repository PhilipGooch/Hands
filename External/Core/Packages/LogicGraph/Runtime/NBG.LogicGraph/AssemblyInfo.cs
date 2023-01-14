using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

[assembly: Preserve]
[assembly: AlwaysLinkAssembly] // For discovery of node definitions via reflection

[assembly: InternalsVisibleTo("NBG.LogicGraph.Editor")]
[assembly: InternalsVisibleTo("Unity.NBG.LogicGraph.CodeGen")]

[assembly: InternalsVisibleTo("NBG.LogicGraph.Tests")]
[assembly: InternalsVisibleTo("NBG.LogicGraph.Editor.Tests")]
[assembly: InternalsVisibleTo("NBG.LogicGraph.CodeGen.Tests")]

[assembly: InternalsVisibleTo("NBG.LogicGraph.EditorUI")]
[assembly: InternalsVisibleTo("NBG.LogicGraph.EditorUI.Tests")]

[assembly: InternalsVisibleTo("NBG.LogicGraph.Net")] // For custom nodes
