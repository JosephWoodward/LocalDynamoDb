using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using LocalDynamoDb.Builder;
using Xunit;
using Xunit.Abstractions;

namespace LocalDynamoDb.Tests.Docker.Fixtures
{
    public class DeleteImageTestFixture : IAsyncLifetime, IDisposable
    {
        private readonly DockerClient _docker;
        private readonly IDynamoInstance _dynamo;
        private const string ImageName = "amazon/dynamodb-local";

        public DeleteImageTestFixture()
        {
            _docker = new DockerClientConfiguration(LocalDockerUri()).CreateClient();
            _dynamo = new LocalDynamoDbBuilder().Container().UsingDefaultImage().ExposePort().Build();
        }

        public IDynamoInstance LocalDynamo 
            => _dynamo;

        public async Task<IList<ImagesListResponse>> ListLocalDynamoImages()
            => await _docker.Images.ListImagesAsync(new ImagesListParameters {MatchName = ImageName});

        public async Task<bool> LocalDynamoImageExists()
        {
            var images = await _docker.Images.ListImagesAsync(new ImagesListParameters {MatchName = ImageName});
            return images.Any();
        }

        public async Task DeleteContainerIfExists()
        {
            var images = await ListLocalDynamoImages();
            if (!images.Any())
                return;
            
            foreach (var tag in images.FirstOrDefault().RepoTags)
            {
                await _docker.Images.DeleteImageAsync(tag, new ImageDeleteParameters
                {
                    Force = true,
                    PruneChildren = true
                });    
            }   
        }
        
        private static Uri LocalDockerUri()
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            return isWindows ? new Uri("npipe://./pipe/docker_engine") : new Uri("unix:/var/run/docker.sock");
        }
        
        public void Dispose()
            => _docker.Dispose();

        public Task InitializeAsync()
            => Task.CompletedTask;

        public Task DisposeAsync()
            => _dynamo.StopAsync();
        
    }
}