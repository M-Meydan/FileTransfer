using FileTransfer.Helpers;
using FileTransfer.Models;
using FileTransfer.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using MMQ;

namespace FileTransfer.Commands
{
    public class TransferCommand : IRequest, ICommand
    {
        public string[] CommandArgs { get; set; }
    }

    public class TransferCommandHandler : IRequestHandler<TransferCommand>
    {
        IFileTransferService _fileTransferService;
        ILogger<TransferCommandHandler> _logger;

        public TransferCommandHandler(IFileTransferService fileTransferService, ILogger<TransferCommandHandler> logger)
        { _fileTransferService = fileTransferService; _logger = logger;}

        /// <summary>
        /// Handles command event
        /// </summary>
        public Task<Unit> Handle(TransferCommand command, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (TryParseArguments(command.CommandArgs, out FileTransferMdl fileTransferMdl) &&
                HasValidPaths(fileTransferMdl))
            {
                _logger.LogInformation($"{Thread.CurrentThread.ManagedThreadId}| Handling TransferCommand");

                string fileGroupName;
                foreach (var fileGroup in fileTransferMdl.GetFileGroups())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    fileGroupName = !string.IsNullOrWhiteSpace(fileGroup.Key) ? fileGroup.Key.ToLower() : "NoExtension";

                    if (_fileTransferService.CreateQueue(fileGroupName, out IMemoryMappedQueue mmQueue, cancellationToken))
                        Task.Factory.StartNew(() => _fileTransferService.StartQueueConsumer(fileGroupName, mmQueue, cancellationToken), TaskCreationOptions.LongRunning);

                    _fileTransferService.EnqueueTransferJobs(fileGroupName, fileTransferMdl.DestFolderPath, fileGroup, cancellationToken);
                }
            }
            
            return Unit.Task;
        }

        private bool TryParseArguments(string[] arguments, out FileTransferMdl fileTransferMdl)
        {
            if (arguments.Length == 2) // expects source and destination parameters
            {
                fileTransferMdl = new FileTransferMdl(arguments[0], arguments[1]);
                return true;
            }

            fileTransferMdl = null;
            _logger.LogInformation("Invalid arguments!");

            return false;
        }

        private bool HasValidPaths(FileTransferMdl fileTransferMdl)
        {
            if (!Directory.Exists(fileTransferMdl.SrcFolderPath))
            {
                _logger.LogInformation($"Source folder does not exist! '{fileTransferMdl.SrcFolderPath}'");
                return false;
            }
            
            if (!Directory.Exists(fileTransferMdl.DestFolderPath))
            {
                try { Directory.CreateDirectory(fileTransferMdl.DestFolderPath); }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Error creating destination folder! '{fileTransferMdl.DestFolderPath}'");
                    _logger.LogInformation($"** {ex.Message}");
                    return false;
                }
            }

            return true;
        }

    }
}
