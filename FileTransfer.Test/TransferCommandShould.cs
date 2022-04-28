using FileTransfer.Commands;
using FileTransfer.Helpers;
using FileTransfer.Models;
using FileTransfer.Services;
using Microsoft.Extensions.Logging;
using MMQ;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileTransfer.Test
{
    public class TransferCommandShould
    {
        Mock<IFileTransferService> _fileTransferService;
        TransferCommandHandler _commandHandler;

        [SetUp]
        public void Setup()
        {
            _fileTransferService = new Mock<IFileTransferService>();
            _commandHandler = new TransferCommandHandler(_fileTransferService.Object, new Mock<ILogger<TransferCommandHandler>>().Object);
        }

        [TestCase(null, "./")]
        [TestCase("./", null)]
        [TestCase("./", "")]
        public async Task Not_start_file_tranfter_when_arguments_invalidAsync(string srcFolder, string destFolder)
        {
            var command = new TransferCommand() { CommandArgs = new string[] { } };
            var fileTransferMdlMock = GetFileTransferMdlMock();
            IMemoryMappedQueue mmQueue;

            _fileTransferService.Setup(x=>x.CreateQueue(It.IsAny<string>(),out mmQueue, default(CancellationToken)));
            
            //Act
            await _commandHandler.Handle(command, default(CancellationToken));

            _fileTransferService.Verify(x => x.CreateQueue(It.IsAny<string>(), out mmQueue, default(CancellationToken)), Times.Never);
        }

        private Mock<IFileTransferMdl> GetFileTransferMdlMock()
        {
            var fileTransferMdlMock = new Mock<IFileTransferMdl>();
            fileTransferMdlMock.SetupProperty(x => x.SrcFolderPath, "./src");
            fileTransferMdlMock.SetupProperty(x => x.DestFolderPath, "./dest");

            var fileList = new List<string>() { "a1.pdf", "b1.jpeg", "b2.jpeg", "c1.log", "c2.log", "c3.log" };
            var fileGroup = fileList.GroupBy(x => Path.GetExtension(x));

            fileTransferMdlMock.Setup(x => x.GetFileGroups()).Returns(fileGroup);

            return fileTransferMdlMock;
        }
    }
}