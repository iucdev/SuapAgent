using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using qoldau.suap.miniagent;
using suap.miniagent;

internal class Program {
    private static void Main(string[] args) {

        if(args.Length > 0) {
            Environment.CurrentDirectory = args[0];
        }


        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddWindowsService(options => {
            options.ServiceName = "Qoldau Alcotrack Agent Service";
        });

        builder.Services.AddSingleton<AgentService>();
        builder.Services.AddHostedService<WindowsBackgroundService>();

        var host = builder.Build();
        host.Run();
    }
}