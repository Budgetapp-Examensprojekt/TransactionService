using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using TransactionService.Models;
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
using System.Reflection.Metadata;
using System.Linq;

namespace TransactionService
{
	public class UpdateTransaction
	{

		[FunctionName("UpdateTransaction")]
		[OpenApiOperation(
		   operationId: "UpdateTransaction",
		   tags: ["Transactions"],
		   Summary = "Update a specific transaction",
		   Description = "This endpoint allows the user to update an existing transaction by its ID. The updated transaction data is provided in the request body."
	   )]
		[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
		[OpenApiParameter(
		   name: "id",
		   In = ParameterLocation.Path,
		   Required = true,
		   Type = typeof(string),
		   Description = "The **ID** of the transaction to be updated."
	   )]
		[OpenApiParameter(
		   name: "userId",
		   In = ParameterLocation.Query,
		   Required = true,
		   Type = typeof(string),
		   Description = "The **User ID** of the transaction to be updated."
	   )]
		[OpenApiRequestBody(
		   contentType: "application/json",
		   bodyType: typeof(Transaction),
		   Required = true,
		   Description = "The updated transaction data"
	   )]
		[OpenApiResponseWithBody(
		   statusCode: HttpStatusCode.OK,
		   contentType: "text/plain",
		   bodyType: typeof(string),
		   Description = "Confirmation that the transaction has been updated successfully."
	   )]
		[OpenApiResponseWithoutBody(
		   statusCode: HttpStatusCode.BadRequest,
		   Description = "Invalid transaction data provided in the request body."
	   )]
		public async Task<IActionResult> Run(
		[HttpTrigger(AuthorizationLevel.Function, "put", Route = "transactions/{id}")] HttpRequest req, string id)
		{
			var cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("CosmosDBConnection"));
			var container = cosmosClient.GetContainer("Cosmos-Piggybanker", "Transactions");

			string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
			var updatedTransaction = JsonConvert.DeserializeObject<Transaction>(requestBody);

			if (updatedTransaction == null || string.IsNullOrEmpty(updatedTransaction.UserId) || updatedTransaction.Amount <= 0 || string.IsNullOrEmpty(updatedTransaction.Category))
			{
				return new BadRequestObjectResult("Please pass a valid transaction object in the request body");
			}

			var sqlQueryText = $"SELECT * FROM c WHERE c.id = @id AND c.userId = @userId";
			var queryDefinition = new QueryDefinition(sqlQueryText).WithParameter("@id", id).WithParameter("@userId", updatedTransaction.UserId);

			var queryResultSetIterator = container.GetItemQueryIterator<Transaction>(queryDefinition);

			if (queryResultSetIterator.HasMoreResults)
			{
				var response = await queryResultSetIterator.ReadNextAsync();
				var existingTransaction = response.FirstOrDefault();

				if (existingTransaction != null)
				{
					existingTransaction.Amount = updatedTransaction.Amount;
					existingTransaction.Category = updatedTransaction.Category;

					await container.ReplaceItemAsync(existingTransaction, existingTransaction.Id, new PartitionKey(existingTransaction.UserId));
					return new OkObjectResult("Transaction update successfully");
				}
			}

			return new NotFoundObjectResult("Transaction not found.");
		}
	}
}




