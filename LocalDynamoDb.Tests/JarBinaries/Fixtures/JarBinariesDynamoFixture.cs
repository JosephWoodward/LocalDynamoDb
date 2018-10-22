using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using LocalDynamoDb.Builder;
using Xunit;

namespace LocalDynamoDb.Tests.JarBinaries.Fixtures
{
    public class JarBinariesDynamoFixture : IAsyncLifetime
    {
        private readonly IDynamoInstance _dynamo;
        private AmazonDynamoDBClient _client;

        public JarBinariesDynamoFixture()
        {
            var builder = new LocalDynamoDbBuilder().JarBinaries().InDefaultPath().OnDefaultPort();
            _dynamo = builder.Build();
        }
        
        public AmazonDynamoDBClient Client
            => _client ?? (_client = _dynamo.CreateClient());

        public Task<LocalDynamoDbState> GetStateAsync()
            => _dynamo.GetStateAsync();

        public async Task InitializeAsync()
        {
            await _dynamo.StartAsync();
        }

        public Task DisposeAsync()
        {
            _client?.Dispose();
            return _dynamo.StopAsync();
        }
    }
}