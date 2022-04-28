using MMQ;

namespace FileTransfer.Test
{
    /// <summary>
    /// Used for unit testing
    /// </summary>
    public interface IMMQWrapper
    {
        public IMemoryMappedQueue Create(string name);
        public IMemoryMappedQueueProducer CreateProducer();
        public IMemoryMappedQueueConsumer CreateConsumer();
    }
}
