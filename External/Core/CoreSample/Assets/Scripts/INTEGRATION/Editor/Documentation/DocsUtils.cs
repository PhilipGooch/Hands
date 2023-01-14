public static class DocsUtils
{
    public static void Prepare()
    {
        var pg = new Microsoft.Unity.VisualStudio.Editor.ProjectGeneration();
        pg.Sync();
    }
}
