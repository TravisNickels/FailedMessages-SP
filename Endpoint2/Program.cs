using System;
using System.Threading.Tasks;
using NServiceBus;

class Program
{
    static async Task Main()
    {
        var endpointName = "Samples-Azure-StorageQueues-Endpoint2";
        Console.Title = endpointName;
        var endpointConfiguration = new EndpointConfiguration(endpointName);
        endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>();
        endpointConfiguration.EnableInstallers();
        endpointConfiguration.SendFailedMessagesTo("error");

        #region DisableRetries

        var recoverability = endpointConfiguration.Recoverability();

        recoverability.Delayed(
            customizations: retriesSettings =>
            {
                retriesSettings.NumberOfRetries(0);
            });
        recoverability.Immediate(
            customizations: retriesSettings =>
            {
                retriesSettings.NumberOfRetries(0);
            });

        #endregion

        var connectionString = Environment.GetEnvironmentVariable("AzureStorageQueue.ConnectionString.SC");
        var transport = new AzureStorageQueueTransport(connectionString)
        {
            QueueNameSanitizer = BackwardsCompatibleQueueNameSanitizer.WithMd5Shortener
        };

        //var transport = new AzureStorageQueueTransport("UseDevelopmentStorage=true")
        //{
        //    QueueNameSanitizer = BackwardsCompatibleQueueNameSanitizer.WithMd5Shortener
        //};
        var routingSettings = endpointConfiguration.UseTransport(transport);
        routingSettings.DisablePublishing();
        endpointConfiguration.UsePersistence<LearningPersistence>();

        var endpointInstance = await Endpoint.Start(endpointConfiguration)
            .ConfigureAwait(false);
        Console.WriteLine("Press any key to exit");
        Console.ReadKey();
        await endpointInstance.Stop()
            .ConfigureAwait(false);
    }
}
