using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace EasybillTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Easybill API Test");
            Console.WriteLine("=================\n");
            
            Console.Write("Bitte API-Key eingeben: ");
            var apiKey = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("Kein API-Key eingegeben!");
                return;
            }
            
            Console.WriteLine("\n--- Test 1: Basic Auth mit Doppelpunkt ---");
            await TestBasicAuth(apiKey);
            
            Console.WriteLine("\n\nDrücken Sie eine Taste zum Beenden...");
            Console.ReadKey();
        }
        
        static async Task TestBasicAuth(string apiKey)
        {
            try
            {
                using var httpClient = new HttpClient();
                
                var baseUrl = "https://api.easybill.de/rest/v1/";
                httpClient.BaseAddress = new Uri(baseUrl);
                
                // Basic Auth mit API-Key als Username, Passwort leer
                var authString = $"{apiKey}:";
                var base64Auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(authString));
                
                httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Basic", base64Auth);
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                
                Console.WriteLine($"Base URL: {baseUrl}");
                Console.WriteLine($"Auth String: {authString}");
                Console.WriteLine($"Base64: {base64Auth}");
                Console.WriteLine($"Full Auth Header: Basic {base64Auth}");
                Console.WriteLine($"Request URL: {baseUrl}customers?limit=1");
                
                var response = await httpClient.GetAsync("customers?limit=1");
                
                Console.WriteLine($"\nHTTP Status: {(int)response.StatusCode} {response.ReasonPhrase}");
                
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response:\n{content}");
                
                if (response.IsSuccessStatusCode)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n✅ ERFOLG!");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n❌ FEHLER!");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Exception: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}
