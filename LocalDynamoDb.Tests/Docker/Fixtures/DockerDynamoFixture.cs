using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using LocalDynamoDb.Builder;
using Xunit;

namespace LocalDynamoDb.Tests.Docker.Fixtures
{
    public class DockerDynamoFixture : IAsyncLifetime
    {
        private readonly IDynamoInstance _dynamo;
        private AmazonDynamoDBClient _client;

        public DockerDynamoFixture()
        {
            var builder = new LocalDynamoDbBuilder().Container().UsingDefaultImage().ExposePort();
            _dynamo = builder.Build();
        }

        public AmazonDynamoDBClient Client
            => _client ?? (_client = _dynamo.CreateClient());

        public Task<LocalDynamoDbState> GetStateAsync()
            => _dynamo.GetStateAsync();

        public Task InitializeAsync() 
            => _dynamo.StartAsync();

        public Task DisposeAsync()
        {
            _client?.Dispose();
            return _dynamo.StopAsync();
        }
    }
}