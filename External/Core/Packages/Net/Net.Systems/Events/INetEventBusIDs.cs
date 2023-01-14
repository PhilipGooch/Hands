using System;

namespace NBG.Net.Systems
{
    /// <summary>
    /// Game specific protocol.
    /// 
    /// Override id values to maintain compatibility between different builds.
    /// </summary>
    public interface INetEventBusIDs
    {
        /// <summary>
        /// Provides a stable id based on serializer type.
        /// </summary>
        /// <returns>0 - no override. Otherwise - network id override.</returns>
        uint GetId(Type eventSerializerType);
    }
}
