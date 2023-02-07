# FailedMessages-SP

OK I think I was able to setup up my dev environment, but it took me a while to do so.
 - Downloaded [ServiceControl](https://particular.net/downloads) and installed it on my pc
 - Created an Azure Storage Account called `servicecontrol`
 - Opened ServiceControl management and connected the audit and monitoring instances to the Azure storage account. 
   - I had originally had errors with this step because I had tried to run ServiceControl from visual studio and there were errors with urlacl reservations. After I ran the following powershell commands to remove the existing instances, I was able to setup the servicecontrol management with the correct instances.  I found this information in our docs here [here](https://docs.particular.net/servicecontrol/powershell) and on stackoverflow [here](https://stackoverflow.com/questions/53501653/netsh-delete-all-urlacl-reservations-of-a-specific-port).
   
   ```ps
   > netsh.exe http show urlacl

   Reserved URL            : http://localhost:33333/
        User: BUILTIN\Users
            Listen: Yes
            Delegate: No
            SDDL: D:(A;;GX;;;BU)

    Reserved URL            : http://+:33334/
        User: BUILTIN\Users
            Listen: Yes
            Delegate: No
            SDDL: D:(A;;GX;;;BU)
    
    > netsh http delete urlacl url=http://localhost:33333/
    > netsh http delete urlacl url=http://+:33334/
   ```
- Cloned the [ServicePulse repo](https://github.com/Particular/ServicePulse) and followed the readme to get the browser up and running.  This went fairly smooth for me.
- Created endpoints to send messages that fail.  This step took me a little while to figure out because the first [sample](https://docs.particular.net/samples/servicecontrol/adapter-asq-multi-storage-account/) I tried I couldn't get it to work.
  - Used [Azure Storage Queues Transport sample](https://docs.particular.net/samples/azure/storage-queues/) as the framework for the endpoints and used [Monitor with ServiceControl events sample](https://docs.particular.net/samples/servicecontrol/events-subscription/) to modify that framework to send failed messages.
  - Disabled StorageReader as a startup project
  - Modified Endpoint 1 and Endpoint 2 to connect to the ASQ `servicecontrol`
  ```cs
    var connectionString = Environment.GetEnvironmentVariable("AzureStorageQueue.ConnectionString.SC");
    var transport = new AzureStorageQueueTransport(connectionString);
  ```
  - Modified Endpoint 2 `Message1Handler.cs` to throw an exception
  ```cs
    public Task Handle(Message1 message, IMessageHandlerContext context)
    {
        log.Info($"Received Message1: {message.Property}");
        throw new Exception("BOOM!");
    }
  ```
  - Modified `program.cs` for Endpoint 2 to send failed messages to the error queue and disable retries
  ```cs
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
  ```
  - This is the result on the running ServicePulse instance
<img width="948" alt="image" src="https://user-images.githubusercontent.com/87037242/217163842-a7e874a1-668f-4fe7-8145-db9b630a91e4.png">
