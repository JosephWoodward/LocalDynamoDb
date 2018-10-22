using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Docker.DotNet;
using Docker.DotNet.Models;
using LocalDynamoDb.Builder;
using Xunit;

namespace LocalDynamoDb.Tests.Docker.Fixtures
{
    public class DeleteImageTestFixture : IDisposable, IAsyncLifetime
    {
        private readonly DockerClient _docker;
        private readonly IDynamoInstance _localDynamo;
        private AmazonDynamoDBClient _dynamo;

        public DeleteImageTestFixture()
        {
            _docker = new DockerClientConfiguration(LocalDockerUri()).CreateClient();
            _localDynamo = new LocalDynamoDbBuilder().Container().UsingDefaultImage().ExposePort().Build();
        }

        public Task StartLocalDynamoAsync()
            => _localDynamo.StartAsync();
        
        public AmazonDynamoDBClient Client
            => _dynamo ?? (_dynamo = _localDynamo.CreateClient());

        public async Task<IList<ImagesListResponse>> ListImages(string imageName)
            => await _docker.Images.ListImagesAsync(new ImagesListParameters { MatchName = imageName });

        public async Task<IList<ImagesListResponse>> ImageExists(string image)
        {
            IList<ImagesListResponse> images = await _docker.Images.ListImagesAsync(new ImagesListParameters {All = true});
            
        }

        public async Task<IList<ContainerListResponse>> Stats()
        {
            return await _docker.Containers.ListContainersAsync(new ContainersListParameters(), CancellationToken.None);
        }

        private async Task DeleteLocalDynamoImage()
        {
            await _docker.Images.DeleteImageAsync("amazon/dynamodb-local", new ImageDeleteParameters
            {
                Force = true,
                PruneChildren = true
            });
        }
        
        private static Uri LocalDockerUri()
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            return isWindows ? new Uri("npipe://./pipe/docker_engine") : new Uri("unix:/var/run/docker.sock");
        }
        
        public void Dispose()
            => _docker.Dispose();

        public Task InitializeAsync()
            => DeleteLocalDynamoImage();

        public Task DisposeAsync()
        {
            throw new NotImplementedException();
        }
    }
}