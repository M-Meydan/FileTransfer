using MediatR;
using Microsoft.Extensions.Logging;

namespace FileTransfer.Commands
{

    public class ExitCommand : ICommand
    {
        public string[] CommandArgs { get; set; }
    }
}
