using Microsoft.Extensions.Configuration;
using System;

namespace FileTransfer.Helpers
{
    public interface IAppConfig
    {
        int BufferLength { get; set; }
    }

    public class AppConfig : IAppConfig
    {
        public AppConfig() { }
        public AppConfig(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            configuration.Bind("AppConfig", this);
        }

        public int BufferLength { get; set; }
    }

}