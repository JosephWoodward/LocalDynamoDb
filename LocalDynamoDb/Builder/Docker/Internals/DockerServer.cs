using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Docker.DotNet;
using Docker.DotNet.Models;
using static System.Threading.CancellationToken;

namespace LocalDynamoDb.Builder.Docker.Internals
{
    internal abstract class DockerServer
    {
        public string ImageName { get; }
        public string ContainerName { get; }
        public LocalDynamoDbState State { get; set; }

        protected DockerServer(string imageName, string containerName)
        {
            ImageName = imageName;
            ContainerName = containerName;
        }

        public async Task<LocalDynamoDbState> GetStateAsync(IDockerClient client)
        {
            var list = await client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true
            }).ConfigureAwait(false);
            
            var container = list.FirstOrDefault(x => x.Names.Contains("/" + ContainerName));
            return container?.State == "running" ? LocalDynamoDbState.Running : State;
        }

        public async Task Start(IDockerClient client)
        {
            if (StartAction != StartAction.None)
                return;
            
            var images = await client.Images.ListImagesAsync(new ImagesListParameters { MatchName = ImageName }).ConfigureAwait(false);
            if (images.Count == 0)
            {
                Console.WriteLine($"Fetching Docker image '{ImageName}'");
                var progress = new Progress<JSONMessage>(x =>
                {
                    Console.WriteLine("Status: " + x.Status);
                });
                
                var cts = new CancellationTokenSource();
                await client.Images.CreateImageAsync(new ImagesCreateParameters { FromImage = ImageName, Tag = "latest" }, new AuthConfig(), progress, cts.Token).ConfigureAwait(false);
            }

            var list = await client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true
            }).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(ContainerName))
            {
                var container = list.FirstOrDefault(x => x.Names.Contains("/" + ContainerName));
                if (container == null)
                {
                    await CreateContainer(client);
                }
                else
                {
                    if (container.State == "running")
                    {
                        Console.WriteLine($"Container '{ContainerName}' is already running.");
                        StartAction = StartAction.External;
                        return;
                    }
                }    
            }

            var started = await client.Containers.StartContainerAsync(ContainerName, new ContainerStartParameters()).ConfigureAwait(false);
            if (!started)
            {
                State = LocalDynamoDbState.Stopped;
                throw new InvalidOperationException($"Container '{ContainerName}' did not start!");
            }

            var i = 0;
            while (true)
            {
                var r = await IsReady().ConfigureAwait(false);
                if (r)
                {
                    Console.WriteLine($"Container '{ContainerName}' is ready.");
                    StartAction = StartAction.Started;
                    State = LocalDynamoDbState.Running;
                    return;
                }
                
                i++;

                if (i > 20)
                {
                    State = LocalDynamoDbState.Stopped;
                    throw new TimeoutException($"Container {ContainerName} does not seem to be responding in a timely manner");
                }

                await Task.Delay(TimeSpan.FromSeconds(2));
            }   
        }

        public static StartAction StartAction { get; private set; } = StartAction.None;

        private async Task CreateContainer(IDockerClient client)
        {
            Console.WriteLine($"Creating container '{ContainerName}' using image '{ImageName}'");

            var hostConfig = ToHostConfig();
            var config = ToConfig();

            await client.Containers.CreateContainerAsync(new CreateContainerParameters(config)
            {
                Image = ImageName,
                Name = ContainerName,
                Tty = true,
                HostConfig = hostConfig,
            });
        }

        public Task StopAsync(IDockerClient client)
        {
            State = LocalDynamoDbState.Stopped;
            return client.Containers.StopContainerAsync(ContainerName, new ContainerStopParameters());
        }

        protected abstract Task<bool> IsReady();

        public abstract HostConfig ToHostConfig();

        public abstract Config ToConfig();

        public override string ToString()
            => $"{nameof(ImageName)}: {ImageName}, {nameof(ContainerName)}: {ContainerName}";
    }
}