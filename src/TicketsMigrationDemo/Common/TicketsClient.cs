using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Contracts
{
    public class TicketsClient
    {
        private readonly HttpClient _client;
        
        public TicketsClient()
        {
            _client = new HttpClient {BaseAddress = new Uri("https://localhost:5001")};
        }

        public async Task<(User, Ticket[], Comment[])> GetAllInfo(Guid userId)
        {
            var user = await GetUser(userId);
            var tickets = await GetTickets(userId);
            
            var tasks = tickets.Select(x => GetComments(x.Id));
            var results = await Task.WhenAll(tasks);
            var comments = results.SelectMany(x => x).ToList();
            
            return (user, tickets, comments.ToArray());
        }
        
        public async Task<User> GetUser(Guid userId)
        {
            var response = await _client.GetAsync($"/users/{userId}");
            response.EnsureSuccessStatusCode();
            
            var body = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<User>(body);
        }
        
        public async Task<Ticket[]> GetTickets(Guid userId)
        {
            var response = await _client.GetAsync($"/users/{userId}/tickets");
            response.EnsureSuccessStatusCode();
            
            var body = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Ticket[]>(body);
        }
        
        public async Task<Comment[]> GetComments(Guid ticketId)
        {
            var response = await _client.GetAsync($"/tickets/{ticketId}/comments");
            response.EnsureSuccessStatusCode();
            
            var body = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Comment[]>(body);
        }
    }
}