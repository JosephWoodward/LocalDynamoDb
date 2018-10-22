using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Docker.DotNet;
using LocalDynamoDb.Builder.Docker.Internals;

namespace LocalDynamoDb.Builder.Docker
{
    internal class DockerDynamoInstance : IDynamoInstance, IDockerDynamoInstance
    {
        private readonly DockerConfiguration _configuration;
        private readonly IDockerClient _dockerClient;
        private readonly DynamoDbContainer _container;
        
        public DockerDynamoInstance(DockerConfiguration configuration)
        {
            _configuration = configuration;
            
            _dockerClient = new DockerClientConfiguration(LocalDockerUri()).CreateClient();
            _container = new DynamoDbContainer(configuration.ImageName, configuration.ContainerName, configuration.PortNumber);
        }
        
        private static Uri LocalDockerUri()
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            return isWindows ? new Uri("npipe://./pipe/docker_engine") : new Uri("unix:/var/run/docker.sock");
        }

        public int PortNumber
            => _configuration.PortNumber; 

        public string ContainerName
            => _configuration.ContainerName;

        public string ImageName
            => _configuration.ImageName;

        public string Tag
            => _configuration.Tag;

        public Task<LocalDynamoDbState> GetStateAsync()
            => _container.GetStateAsync(_dockerClient);

        public Task<bool> StartAsync()
            => StartAsync(default);
        
        public Task<bool> StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _container.StartAsync(_dockerClient).Wait(TimeSpan.FromSeconds(60));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return Task.FromResult(true);
        }

        public Task StopAsync()
            => _container.StopAsync(_dockerClient);

        public AmazonDynamoDBClient CreateClient()
        {
            var config = new AmazonDynamoDBConfig { ServiceURL = $"http://localhost:{_configuration.PortNumber}"};
            var credentials = new BasicAWSCredentials("A NIGHTINGALE HAS NO NEED FOR KEYS", "IT OPENS DOORS WITH ITS SONG");
            
            return new AmazonDynamoDBClient(credentials, config);
        }
    }
}