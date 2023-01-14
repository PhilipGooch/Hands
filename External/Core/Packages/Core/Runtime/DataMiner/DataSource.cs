using System.IO;

namespace NBG.Core.DataMining
{
    public interface IDataBlob
    {
    }

    // Will be called during FixedUpdate
    public interface IDataSourceFixedUpdater
    {
        void OnFixedUpdate();
    }

    // Will be called during LateUpdate
    public interface IDataSourceLateUpdater
    {
        void OnLateUpdate();
    }

    public interface IDataSource
    {
        byte Id { get; } // Globally unique data source ID
        byte Version { get; } // Current (written) version
        bool FrameUnique { get; } // Max one per frame if true

        void OnBeginRecording();
        void OnEndRecording();

        void Write(BinaryWriter writer);
        IDataBlob Read(BinaryReader reader, byte version);
    }
}
