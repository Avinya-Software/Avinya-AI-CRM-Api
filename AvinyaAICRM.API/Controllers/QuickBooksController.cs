using AvinyaAICRM.Domain.Entities.QuickBook;
using AvinyaAICRM.Infrastructure.Persistence;
using AvinyaAICRM.Shared.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AvinyaAICRM.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuickBooksController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public QuickBooksController(IConfiguration configuration, AppDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        [HttpGet("connect")]
        public IActionResult Connect()
        {
            var clientId = _configuration["QuickBooks:ClientId"];
            var redirectUri = _configuration["QuickBooks:RedirectUri"];

            var url = $"https://appcenter.intuit.com/connect/oauth2" +
                      $"?client_id={clientId}" +
                      $"&response_type=code" +
                      $"&scope=com.intuit.quickbooks.accounting" +
                      $"&redirect_uri={redirectUri}" +
                      $"&state=12345";

            return Redirect(url);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback(string code, string realmId)
        {
            var clientId = _configuration["QuickBooks:ClientId"];
            var clientSecret = _configuration["QuickBooks:ClientSecret"];
            var redirectUri = _configuration["QuickBooks:RedirectUri"];

            var client = new HttpClient();

            var authValue = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")
            );

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", authValue);

            var body = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("grant_type","authorization_code"),
                new KeyValuePair<string,string>("code",code),
                new KeyValuePair<string,string>("redirect_uri",redirectUri)
            });

            var response = await client.PostAsync(
                "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer",
                body
            );

            var result = await response.Content.ReadAsStringAsync();

            var tokenObj = JsonSerializer.Deserialize<JsonElement>(result);

            var accessToken = tokenObj.GetProperty("access_token").GetString();
            var refreshToken = tokenObj.GetProperty("refresh_token").GetString();
            var expiresIn = tokenObj.GetProperty("expires_in").GetInt32();

            var expiryDate = DateTime.Now.AddSeconds(expiresIn);

            var existing = await _context.QuickBooksConnections
                .FirstOrDefaultAsync(x => x.RealmId == realmId);

            if (existing != null)
            {
                existing.AccessToken = accessToken;
                existing.RefreshToken = refreshToken;
                existing.TokenExpiry = expiryDate;
                existing.UpdatedDate = DateTime.Now;
            }
            else
            {
                _context.QuickBooksConnections.Add(new QuickBooksConnection
                {
                    RealmId = realmId,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    TokenExpiry = expiryDate,
                    CreatedDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            return Redirect("https://aicrm.avinyasoftware.com/quickbooks-success");
        }

        [HttpGet("customers")]
        public async Task<ResponseModel> GetCustomers()
        {
            ResponseModel response = new ResponseModel();

            try
            {
                var connection = await _context.QuickBooksConnections
                    .FirstOrDefaultAsync();

                if (connection == null)
                {
                    response.StatusCode = 400;
                    response.StatusMessage = "QuickBooks not connected.";
                    return response;
                }

                var realmId = connection.RealmId;
                var accessToken = connection.AccessToken;

                var url = $"https://sandbox-quickbooks.api.intuit.com/v3/company/{realmId}/query?query=select * from Customer";

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                var result = await client.GetAsync(url);
                var content = await result.Content.ReadAsStringAsync();

                response.StatusCode = 200;
                response.StatusMessage = "Customers fetched successfully";
                response.Data = JsonSerializer.Deserialize<object>(content);

                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = ex.Message;
                return response;
            }
        }
        [HttpPost("customer")]
        public async Task<ResponseModel> CreateCustomer([FromBody] object customerData)
        {
            ResponseModel response = new ResponseModel();

            try
            {
                var connection = await _context.QuickBooksConnections.FirstOrDefaultAsync();

                if (connection == null)
                {
                    response.StatusCode = 400;
                    response.StatusMessage = "QuickBooks not connected.";
                    return response;
                }

                var url = $"https://sandbox-quickbooks.api.intuit.com/v3/company/{connection.RealmId}/customer";

                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", connection.AccessToken);
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                var json = JsonSerializer.Serialize(customerData);

                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var result = await client.PostAsync(url, httpContent);

                var content = await result.Content.ReadAsStringAsync();

                response.StatusCode = 200;
                response.StatusMessage = "Customer created successfully";
                response.Data = JsonSerializer.Deserialize<object>(content);

                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = ex.Message;
                return response;
            }
        }
        [HttpGet("invoices")]
        public async Task<ResponseModel> GetInvoices()
        {
            ResponseModel response = new ResponseModel();

            try
            {
                var connection = await _context.QuickBooksConnections.FirstOrDefaultAsync();

                if (connection == null)
                {
                    response.StatusCode = 400;
                    response.StatusMessage = "QuickBooks not connected.";
                    return response;
                }

                var url = $"https://sandbox-quickbooks.api.intuit.com/v3/company/{connection.RealmId}/query?query=select * from Invoice";

                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", connection.AccessToken);
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                var result = await client.GetAsync(url);

                var content = await result.Content.ReadAsStringAsync();

                response.StatusCode = 200;
                response.StatusMessage = "Invoices fetched successfully";
                response.Data = JsonSerializer.Deserialize<object>(content);

                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = ex.Message;
                return response;
            }
        }
        [HttpPost("invoice")]
        public async Task<ResponseModel> CreateInvoice([FromBody] object invoiceData)
        {
            ResponseModel response = new ResponseModel();

            try
            {
                var connection = await _context.QuickBooksConnections.FirstOrDefaultAsync();

                if (connection == null)
                {
                    response.StatusCode = 400;
                    response.StatusMessage = "QuickBooks not connected.";
                    return response;
                }

                var url = $"https://sandbox-quickbooks.api.intuit.com/v3/company/{connection.RealmId}/invoice";

                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", connection.AccessToken);
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                var json = JsonSerializer.Serialize(invoiceData);

                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var result = await client.PostAsync(url, httpContent);

                var content = await result.Content.ReadAsStringAsync();

                response.StatusCode = 200;
                response.StatusMessage = "Invoice created successfully";
                response.Data = JsonSerializer.Deserialize<object>(content);

                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.StatusMessage = ex.Message;
                return response;
            }
        }
    }
}