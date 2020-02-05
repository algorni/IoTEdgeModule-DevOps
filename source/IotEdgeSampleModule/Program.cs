namespace IotEdgeSampleModule
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Shared;

    class Program
    {
        //static string localStorageConnectionString = Environment.GetEnvironmentVariable("LOCAL_STORAGE_CONNECTION_STRING");

        static ModuleClient ioTHubModuleClient = null;

        static void Main(string[] args)
        {
            //initialize module client
            Init().Wait();


            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();

            //Start the infinite loop task :)
            Task.Run(() => InfiniteLoop(cts.Token));

            WhenCancelled(cts.Token).Wait();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }


        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init()
        {
            AmqpTransportSettings amqpSetting = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            ITransportSettings[] settings = { amqpSetting };

            // Open a connection to the Edge runtime
            ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();

            Twin twin = ioTHubModuleClient.GetTwinAsync().Result;

            string jsonString = twin.ToJson();

            Console.WriteLine("---- Initial Twin ---");
            Console.WriteLine(jsonString);
            Console.WriteLine("---------------------");

            //Just register for handling the desired property update
            await ioTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallBackMethod, ioTHubModuleClient);

            Console.WriteLine("IoT Hub module client initialized.");
        }

        static Task DesiredPropertyUpdateCallBackMethod(TwinCollection desiredProperties, object userContext)
        {
            string jsonString = desiredProperties.ToJson();

            Console.WriteLine("---- Twin Update ---");
            Console.WriteLine(jsonString);
            Console.WriteLine("--------------------");

            return Task.CompletedTask;
        }

        static void InfiniteLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Twin twin = ioTHubModuleClient.GetTwinAsync().Result;

                string jsonString = twin.ToJson();

                Console.WriteLine("---- Twin Loop   ---");
                Console.WriteLine(jsonString);
                Console.WriteLine("--------------------");

                Task.Delay(15000).Wait();
            }
        }

        /*
        static void BlobStuff(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Connecting to local Blob Storage: {localStorageConnectionString}");
            try
            {

                // Create a BlobServiceClient object which will be used to create a container client
                BlobServiceClient blobServiceClient = new BlobServiceClient(localStorageConnectionString);

                Console.WriteLine("Connected");

                //Create a unique name for the container
                string containerName = "edgeblobcontainer";

                // Create the container and return a container client object
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                while (!cancellationToken.IsCancellationRequested)
                {
                    DateTime now = DateTime.UtcNow;
                    string fileName = now.ToString().Replace('/', '-').Replace('\\', '-');

                    Console.WriteLine($"Writing a new blob {fileName}");

                    // Get a reference to a blob
                    BlobClient blobClient = containerClient.GetBlobClient($"{fileName}");

                    byte[] byteArray = Encoding.UTF8.GetBytes("test 123 456");

                    MemoryStream stream = new MemoryStream(byteArray);

                    blobClient.Upload(stream);

                    Task.Delay(60000).Wait();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during the infinite loop: {ex.ToString()}");

                throw;
            }
          
        }
        */
    }
}
