using FileTransfer.Helpers;
using Microsoft.Extensions.Logging;
using MMQ;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text;

namespace FileTransfer.Services
{
    public interface IFileTransferService
    {
        ConcurrentDictionary<string, IMemoryMappedQueueProducer> GetProcessingQueues();
        bool CreateQueue(string queueName, out IMemoryMappedQueue mmQueue, CancellationToken cancellationToken);
        void EnqueueTransferJobs(string queueName, string destFolderPath, IGrouping<string, string> fileGroup, CancellationToken cancellationToken);
        void StartQueueConsumer(string queueName, IMemoryMappedQueue mmQueue, CancellationToken cancellationToken);
    }

    public class FileTransferService : IFileTransferService
    {
        ConcurrentDictionary<string, IMemoryMappedQueueProducer> _processingQueues = new ConcurrentDictionary<string, IMemoryMappedQueueProducer>();

        readonly ILogger<IFileTransferService> _logger;
        readonly IAppConfig _appConfig;

        public FileTransferService(IAppConfig appConfig,  ILogger<IFileTransferService> logger)
        {
            _logger = logger;
            _appConfig = appConfig;
        }

        ///// <summary>
        ///// Creates a ProcessingQueue for each file extension so same files processed all together
        ///// </summary>
        public bool CreateQueue(string queueName,out IMemoryMappedQueue mmQueue, CancellationToken cancellationToken)
        {
            mmQueue = null;
            cancellationToken.ThrowIfCancellationRequested();

            if (!_processingQueues.TryGetValue(queueName, out IMemoryMappedQueueProducer mmQueueProducer))
            {
                mmQueue = MemoryMappedQueue.Create(queueName);
                mmQueueProducer= mmQueue.CreateProducer();

                _processingQueues.TryAdd(queueName, mmQueueProducer);

                _logger.LogInformation($"{Thread.CurrentThread.ManagedThreadId}| Processing Queue Created: {queueName}");

               return true;
            }

            return false;
        }

        /// <summary>
        /// Queues transfer jobs for the consumer
        /// </summary>
        public void EnqueueTransferJobs(string queueName, string destFolderPath, IGrouping<string, string> fileGroup, CancellationToken cancellationToken)
        {
            string destFilePath;

            if (_processingQueues.TryGetValue(queueName, out IMemoryMappedQueueProducer mmQueueProducer))
            {
                foreach (var filePath in fileGroup)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    destFilePath = Path.Combine(destFolderPath, Path.GetFileName(filePath));
                    var message = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new KeyValuePair<string, string>(filePath, destFilePath)));
                    mmQueueProducer.Enqueue(message);
                }
            }
        }

        public void StartQueueConsumer(string queueName, IMemoryMappedQueue mmQueue, CancellationToken cancellationToken)
        {
            //Task.Delay(5000).Wait();

            var mmQueueConsumer = mmQueue.CreateConsumer();
            _logger.LogInformation($"{Thread.CurrentThread.ManagedThreadId}| Starting Consumer: {queueName}");

            while (!cancellationToken.IsCancellationRequested)
            {
                if (mmQueueConsumer.TryDequeue(out byte[] message))
                {
                    var text = Encoding.UTF8.GetString(message);
                    var transferePaths = JsonConvert.DeserializeObject<KeyValuePair<string, string>>(text);
                    _logger.LogInformation($"{Thread.CurrentThread.ManagedThreadId}|   Transferring:{transferePaths.Key} =>{transferePaths.Value}");

                    try
                    {
                        TransferFile(transferePaths.Key, transferePaths.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error transferring file:{transferePaths.Key}|{transferePaths.Value} => { ex.Message}");
                    }
                }
                else
                    _logger.LogInformation($"awaiting messages for queue: {queueName}");
            }
        }

        public ConcurrentDictionary<string, IMemoryMappedQueueProducer> GetProcessingQueues()
        {
            return _processingQueues;
        }

        private void TransferFile(string srcFilePath, string destFilePath)
        {
            using (FileStream fsOut = File.OpenWrite(destFilePath))
            {
                using (FileStream fsIn = File.OpenRead(srcFilePath))
                {
                    byte[] buffer = new byte[_appConfig.BufferLength];
                    int bytesRead;
                    do
                    {
                        bytesRead = fsIn.Read(buffer, 0, buffer.Length);
                        fsOut.Write(buffer, 0, bytesRead);
                    } while (bytesRead != 0);
                }
            }
        }

    }
}
