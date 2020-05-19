using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Proceficator.Schema;
using Procsender.Schema;
using Newtonsoft.Json;
using Confluent.Kafka;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Proceficator {
  class Program {
    static async Task Main() {
      Console.Out.WriteLine($"Proceficator Ver. {Assembly.GetExecutingAssembly().GetName().Version.ToString()}");
      Task handler = Task.Run(DBHandler.HandleQueue);
      BuildWebHost().Run();

      await handler;
    }

    public static IWebHost BuildWebHost() {
      var config = new ConfigurationBuilder()
          .SetBasePath(Directory.GetCurrentDirectory())
          .AddJsonFile(@"appsettings.json", false, true)
          //.AddEnvironmentVariables(prefix: "ASPNETCORE_")
          //.AddCommandLine(args)
          .Build();

      return new WebHostBuilder()
          .UseConfiguration(config)
          .ConfigureLogging(builder => {
            builder.SetMinimumLevel(LogLevel.Warning);
            builder.AddConfiguration(config);
            builder.AddConsole();
          })
          .UseKestrel()
          .UseStartup(typeof(BasicStartup))
          .Build();
    }
  }

  internal static class DBHandler {

    internal static async Task HandleQueue() {
      int delay = 1000;
      while (true) {
        try {
          List<TSelect> queue = DataAccess.Get_Transactions();
          foreach (TSelect select in queue) {
            var form = new NFrom() {
              Uri = select.config.uri,
              Headers = select.config.headers,
              HttpMethod = select.config.method,
              Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(select.transaction))
            };
            DataAccess.Set_Status_Queue(select.queue.id, Queue.Status.Execution);
            await PClient.QProduce(select.queue.id, JsonConvert.SerializeObject(form));
          }
        } catch (Exception e) {
          Console.Out.WriteLine($"Could not catch the queue: {e.Message}, {e.StackTrace}");
        } finally {
          await Task.Delay(delay);
        }
      }
    }
  }
}