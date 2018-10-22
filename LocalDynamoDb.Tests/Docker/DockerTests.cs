using System;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using LocalDynamoDb.Builder;
using LocalDynamoDb.Tests.Docker.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace LocalDynamoDb.Tests.Docker
{
    public class DockerTests : IClassFixture<DeleteImageTestFixture>, IDisposable
    {
        private readonly DeleteImageTestFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly AmazonDynamoDBClient _dynamoClient;

        public DockerTests(DeleteImageTestFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }
        
        /*[Fact]*/
        public async Task PullsContainer()
        {
            await _fixture.StartLocalDynamoAsync();
        }
        
        public void Dispose()
            => _dynamoClient.Dispose();
    }
}