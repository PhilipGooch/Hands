using NUnit.Framework;

namespace NBG.Core.Editor.Tests
{
    public class SerializableGuidTests
    {
        [Test]
        public void NewGuidIsEmpty()
        {
            var guid = new SerializableGuid();
            Assert.IsTrue(guid == SerializableGuid.empty);
        }

        [Test]
        public void ConversionFromAndToSystemGUIDWorks()
        {
            var systemGuid = System.Guid.NewGuid();
            var serializableGuid = SerializableGuid.Create(systemGuid);
            Assert.IsTrue(serializableGuid.guid == systemGuid);
        }
    }
}
