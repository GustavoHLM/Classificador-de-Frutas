using Microsoft.Azure.Cosmos;
using System;
using System.Threading.Tasks;

public class CosmosDBService
{
    private string EndpointUrl = "https://bancoimg.documents.azure.com:443/";
    private string AuthorizationKey = "cHl0pMxOIyltMoLdTfZEnufvnp2GjT3RuLdcau4NJ5xYvcvEsjmy7R9kZX6rjCCljvNCJOrznIlGACDbFawXbg==";
    private string DatabaseId = "bancoimg";
    private string ContainerId = "SuaContainerId";

    public async Task UpsertDocumentAsync(object document, string Usecontainer)
    {
        using (var cosmosClient = new CosmosClient(EndpointUrl, AuthorizationKey))
        {
            var database = cosmosClient.GetDatabase(DatabaseId);
            var container = database.GetContainer(Usecontainer);
            var response = await container.UpsertItemAsync(document);
        }
    }
}
