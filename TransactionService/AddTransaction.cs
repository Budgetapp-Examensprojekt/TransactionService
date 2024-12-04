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
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using TransactionService.Models;

namespace TransactionService
{
    public class AddTransaction()
	{

		[FunctionName("AddTransaction")]
		[OpenApiOperation(
					operationId: "AddTransaction",
					tags: ["Transactions"],
					Summary = "Add a new transaction",
					Description = "This endpoint adds a new transaction to the Cosmos DB.")]
		[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
		[OpenApiRequestBody(
					contentType: "application/json",
					bodyType: typeof(Transaction),
					Required = true,
					Description = "The transaction details to be added")]
		[OpenApiResponseWithBody(
					statusCode: HttpStatusCode.OK,
					contentType: "application/json",
					bodyType: typeof(string),
					Description = "The confirmation that the transaction was successfully added.")]
		[OpenApiResponseWithBody(
					statusCode: HttpStatusCode.BadRequest,
					contentType: "application/json",
					bodyType: typeof(string),
					Description = "The error message if the input is invalid.")]
		public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "transactions")] HttpRequest req)
        {
            var cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("CosmosDBConnection"));
            var container = cosmosClient.GetContainer("Cosmos-Piggybanker", "Transactions");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var transaction = JsonConvert.DeserializeObject<Transaction>(requestBody);

            if (transaction == null)
            {
				return new BadRequestObjectResult("Please pass a valid transaction object in the request body");
			}

			transaction.Id = Guid.NewGuid().ToString();

			try
			{
				await container.CreateItemAsync(transaction, new PartitionKey(transaction.UserId));
				return new OkObjectResult("Transaction added successfully");
			}
			catch (Exception ex)
			{
				return new BadRequestObjectResult($"Error adding transaction: {ex.Message}");
			}
		}
    }
}

