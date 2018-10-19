using System;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using LocalDynamoDb.Builder;
using LocalDynamoDb.Builder.Docker;

namespace LocalDynamoDb.Tests.Docker.Fixtures
{
    public class DockerDynamoFixture : IDisposable
    {
        private readonly IDynamoInstance _dynamo;
        private AmazonDynamoDBClient _client;

        public DockerDynamoFixture()
        {
            var builder = new LocalDynamoDbBuilder().Container().UsingDefaultImage().ExposePort();
            _dynamo = builder.Build();

            Task.Run(() => _dynamo.StartAsync().ConfigureAwait(false)).Wait();
        }

        public AmazonDynamoDBClient Client
            => _client ?? (_client = _dynamo.CreateClient());

        public Task<LocalDynamoDbState> GetStateAsync()
            => _dynamo.GetStateAsync();

        public void Dispose()
        {
            Task.Run(() => _dynamo.StopAsync().ConfigureAwait(false)).Wait();
            _client?.Dispose();
        }
    }
}