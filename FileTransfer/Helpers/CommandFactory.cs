using FileTransfer.Commands;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Helpers
{

    public interface ICommandFactory { ICommand GetCommand(string commandString); }

    /// <summary>
    /// Creates commands from instructions
    /// </summary>
    public class CommandFactory : ICommandFactory
    {
        ILogger<CommandFactory> _logger;

        readonly IDictionary<string, ICommand> _commands = new Dictionary<string, ICommand>
        {
            { "transfer", new TransferCommand() },
            { "exit", new ExitCommand() }
        };

        public CommandFactory(ILogger<CommandFactory> logger) { _logger = logger; }


        public ICommand GetCommand(string commandString)
        {
            if (!string.IsNullOrWhiteSpace(commandString))
            {
                var commandArgs = commandString.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x));

                if (_commands.TryGetValue(commandArgs.First().ToLower(), out ICommand command))
                {
                    command.CommandArgs = commandArgs.Skip(1).ToArray();
                    return command;
                }
            }

            _logger.LogInformation($"Invalid command: '{commandString}' {Environment.NewLine}");
            return null;
        }
    }
}
