using System.Threading.Tasks;
using Amazon.DynamoDBv2;

namespace LocalDynamoDb.Builder
{        
    public interface IDynamoInstance
    {
        AmazonDynamoDBClient CreateClient();
        
        Task<bool> StartAsync();
        
        Task StopAsync();
        
        Task<LocalDynamoDbState> GetStateAsync();
    }
}