using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using TransactionService.Models;

namespace TransactionService
{
    public class DeleteTransaction
    {
        private readonly ILogger<DeleteTransaction> _logger;

        public DeleteTransaction(ILogger<DeleteTransaction> log)
        {
            _logger = log;
        }

		[FunctionName("DeleteTransaction")]
		[OpenApiOperation(operationId: "DeleteTransaction", tags: new[] { "Transactions" },
			Summary = "Delete a transaction",
			Description = "This endpoint deletes a transaction based on its ID and userId.")]
		[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
		[OpenApiParameter(name: "id", In = ParameterLocation.Query, Required = true, Type = typeof(string),
			Description = "The **ID** of the transaction to be deleted")]
		[OpenApiParameter(name: "userId", In = ParameterLocation.Query, Required = true, Type = typeof(string),
			Description = "The **User ID** associated with the transaction (used for partition key)")]
		[OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK,
			Description = "The transaction was deleted successfully.")]
		[OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound,
			Description = "The transaction with the given ID was not found.")]
		public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "transactions/{id}/{userId}")] HttpRequest req, ILogger log)
        {
            var cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("CosmosDBConnection"));
            var container = cosmosClient.GetContainer("Cosmos-Piggybanker", "Transactions");

            await container.DeleteItemAsync<Transaction>(req.Query["id"], new PartitionKey(req.Query["UserId"]));

			return new OkObjectResult($"Transaction with ID: {req.Query["id"]} deleted successfully");
		}
    }
}

