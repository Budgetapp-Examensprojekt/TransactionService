using System;
using System.Collections.Generic;
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
    public class GetTransactions
    {
        private readonly ILogger<GetTransactions> _logger;

        public GetTransactions(ILogger<GetTransactions> log)
        {
            _logger = log;
        }


		[FunctionName("GetTransactions")]
		[OpenApiOperation(
			operationId: "GetTransactions",
			tags: new[] { "Transactions" },
			Summary = "Retrieve all transactions for a user",
			Description = "This endpoint retrieves all transactions associated with a specific user ID."
		)]
		[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
		[OpenApiParameter(
			name: "userId",
			In = ParameterLocation.Path,
			Required = true,
			Type = typeof(string),
			Description = "The **User ID** for which transactions are being retrieved."
		)]
		[OpenApiResponseWithBody(
			statusCode: HttpStatusCode.OK,
			contentType: "application/json",
			bodyType: typeof(List<Transaction>),
			Description = "The list of transactions for the specified user."
		)]
		[OpenApiResponseWithoutBody(
			statusCode: HttpStatusCode.NotFound,
			Description = "No transactions found for the specified user."
		)]
		public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "transactions/{userId}")] HttpRequest req, ILogger log, string userId)
        {
            var cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("CosmosDBConnection"));
            var container = cosmosClient.GetContainer("Cosmos-Piggybanker", "Transactions");

            var query = new QueryDefinition("SELECT * FROM t WHERE t.userId = @userId")
				.WithParameter("@userId", userId);
            var iterator = container.GetItemQueryIterator<Transaction>(query);

            var transactions = new List<Transaction>();
			while (iterator.HasMoreResults)
			{
				var response = await iterator.ReadNextAsync();
				transactions.AddRange(response);
			}

			return new OkObjectResult(transactions);
		}
    }
}

