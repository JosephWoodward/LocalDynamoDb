using System.Threading.Tasks;
using LocalDynamoDb.Builder;
using LocalDynamoDb.Tests.Docker.Fixtures;
using Shouldly;
using Xunit;

namespace LocalDynamoDb.Tests.Docker
{
    public class DockerTests : IClassFixture<DeleteImageTestFixture>
    {
        private readonly DeleteImageTestFixture _fixture;

        public DockerTests(DeleteImageTestFixture fixture)
        {
            _fixture = fixture;
        }
        
        [Fact]
        public async Task PullsContainer()
        {
            // Arrange
            await _fixture.DeleteContainerIfExists();

            // Act
            await _fixture.LocalDynamo.StartAsync();
            
            // Assert
            var state = await _fixture.LocalDynamo.GetStateAsync();
            state.ShouldBe(LocalDynamoDbState.Running);
        }
    }
}