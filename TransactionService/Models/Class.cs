using Newtonsoft.Json;
using System;

namespace TransactionService.Models;

public class Transaction
{
	[JsonProperty("id")]
	public string Id { get; set; }
	[JsonProperty("userId")]
	public string UserId { get; set; }
	[JsonProperty("amount")]
	public double Amount { get; set; }
	[JsonProperty("category")]
	public string Category { get; set; }
	[JsonProperty("date")]
	public DateTime Date { get; set; }
}
