using System.Threading.Tasks;

namespace Automation.Core
{
    public interface IUnityInstallationTasks
    {
        /// <summary>
        /// Path to the Unity editor executable
        /// </summary>
        /// <returns>null if not found</returns>
        Task<string> GetUnityEditorExecutable(string version);
        Task<int> InstallUnityEditor(string version, string changeset);
        Task<int> InstallUnityEditorModule(string version, string changeset, string module);
    }
}
