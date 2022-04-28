using FileTransfer.Helpers;
using FileTransfer.Models;
using FileTransfer.Services;
using Microsoft.Extensions.Logging;
using MMQ;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileTransfer.Test
{
    public class FileTransferServiceShould
    {
        IFileTransferService _fileTransferService;
        Mock<IMemoryMappedQueueProducer> _mmQueueProducer;

        [SetUp]
        public void Setup()
        {
            _mmQueueProducer = new Mock<IMemoryMappedQueueProducer>();
            _fileTransferService = new FileTransferService(new Mock<IAppConfig>().Object, new Mock<ILogger<IFileTransferService>>().Object);
        }

        [Test()]
        public void Create_queue_for_same_files_types()
        {
            var fileTransferMdlMock = GetFileTransferMdlMock();
            var processingQueues = _fileTransferService.GetProcessingQueues();

            //Act
            foreach (var group in fileTransferMdlMock.Object.GetFileGroups())
                _fileTransferService.CreateQueue(group.Key, out IMemoryMappedQueue mmQueue, default(CancellationToken));

            //Queue by file types should be created
            Assert.True(fileTransferMdlMock.Object.GetFileExtensions()
                        .All(queueName => processingQueues.ContainsKey(queueName)));
        }


        [TestCase(".pdf")]
        [TestCase(".jpeg")]
        [TestCase(".log")]
        public void Enqueue_messages_on_producer_for_same_files_types(string queueName)
        {
            var mmQueueWrapper = new Mock<IMMQWrapper>();  
            var mmQueueProducer = new Mock<IMemoryMappedQueueProducer>();
            var fileTransferMdlMock = GetFileTransferMdlMock();
            var logfileCount = fileTransferMdlMock.Object.GetFileGroups()
                                    .ToDictionary(x => x.Key, x => x.Select(y => y))[queueName].Count();

            mmQueueWrapper.Setup(x => x.Create(queueName)).Returns(new Mock<IMemoryMappedQueue>().Object);
            mmQueueWrapper.Setup(x => x.CreateProducer()).Returns(mmQueueProducer.Object);

            _fileTransferService.GetProcessingQueues().TryAdd(queueName, mmQueueProducer.Object);

            //Act
            foreach (var group in fileTransferMdlMock.Object.GetFileGroups())
                _fileTransferService.EnqueueTransferJobs(group.Key, fileTransferMdlMock.Object.DestFolderPath, group, default(CancellationToken));

            // number of same type files should match the messages enqueued
            mmQueueProducer.Verify(x=>x.Enqueue(It.IsAny<byte[]>()), Times.Exactly(logfileCount));
        }

        [Test()]
        public void Cancellation_request_stops_consumer_processing_messages()
        {
            var cts = new CancellationTokenSource();

            var mmQueue = new Mock<IMemoryMappedQueue>();
            var mmQueueConsumer = new Mock<IMemoryMappedQueueConsumer>();

            mmQueue.Setup(x => x.CreateConsumer()).Returns(mmQueueConsumer.Object);
            cts.Cancel();

            //Act
            _fileTransferService.StartQueueConsumer("Key", mmQueue.Object, cts.Token);

            mmQueue.Verify(x => x.CreateConsumer());
            mmQueueConsumer.Verify(x => x.TryDequeue(out It.Ref<byte[]>.IsAny),Times.Never);
        }

        private Mock<IFileTransferMdl> GetFileTransferMdlMock()
        {
            var fileTransferMdlMock = new Mock<IFileTransferMdl>();
            fileTransferMdlMock.SetupProperty(x => x.SrcFolderPath,"./src");
            fileTransferMdlMock.SetupProperty(x => x.DestFolderPath, "./dest");

            var fileList = new List<string>() { "a1.pdf", "b1.jpeg", "b2.jpeg", "c1.log", "c2.log", "c3.log" };
            var fileGroup = fileList.GroupBy(x => Path.GetExtension(x));

            fileTransferMdlMock.Setup(x => x.GetFileGroups()).Returns(fileGroup);

            return fileTransferMdlMock;
        }
    }
}