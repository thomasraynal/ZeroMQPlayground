using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroMQPlayground.PubSub
{
    public class DirectoryStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IDirectory, Directory>();
        }

    }
}
