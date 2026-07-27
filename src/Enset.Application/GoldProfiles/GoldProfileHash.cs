using System.Security.Cryptography;using System.Text;using System.Text.Json;
namespace Enset.Application.GoldProfiles;
public static class GoldProfileHash{public static (string Json,string Hash) Create<T>(T snapshot){var json=JsonSerializer.Serialize(snapshot,new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase});return(json,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant());}}
