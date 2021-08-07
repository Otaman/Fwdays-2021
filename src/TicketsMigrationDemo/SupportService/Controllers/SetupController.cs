using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Contracts;
using Microsoft.AspNetCore.Mvc;
using SupportService.Database;

namespace SupportService.Controllers
{
    [ApiController]
    public class SetupController : ControllerBase
    {
        private static readonly Random Random = new (42);
        private readonly TicketsContext _context;

        public SetupController(TicketsContext context)
        {
            _context = context;
        }
        
        [HttpGet("/tickets/generate")]
        public async Task<int> GenerateTickets()
        {
            var userIds = UserIdGenerator.GenerateIds().Take(100000).ToArray();
            
            var batchSize = 1000;
            var userIdsBatch = new Guid[batchSize];
            var lastIndex = 0;
            
            do
            {
                Array.Copy(userIds, lastIndex, userIdsBatch, 0, batchSize);
                await CreateUsers(userIdsBatch);
                lastIndex += batchSize;
            } while (lastIndex < userIds.Length - 1);

            return userIds.Length;
        }

        private async Task CreateUsers(Guid[] userIds)
        {
            var users = userIds.Select(GenerateUser).ToArray();
            var tickets = users.Select(GenerateTickets).SelectMany(x => x).ToArray();
            var comments = tickets.Select(GenerateComments).SelectMany(x => x).ToArray();

            await using var ctx = new TicketsContext();
            await ctx.Users.AddRangeAsync(users);
            await ctx.Tickets.AddRangeAsync(tickets);
            await ctx.Comments.AddRangeAsync(comments);
            await ctx.SaveChangesAsync();
        }

        private User GenerateUser(Guid userId) => new User
        {
            Id = userId,
            Email = $"{userId}@gmail.com",
            Name = $"Name {userId}"
        };

        private IEnumerable<Ticket> GenerateTickets(User user)
        {
            var length = Random.Next(5);
            for (int i = 0; i < length; i++)
            {
                yield return new Ticket
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    Title = "New Ticket",
                    Description = $"Ticket #{i} from {user.Email}"
                };
            }
        }
        
        private IEnumerable<Comment> GenerateComments(Ticket ticket)
        {
            var length = Random.Next(5);
            for (int i = 0; i < length; i++)
            {
                yield return new Comment
                {
                    Id = Guid.NewGuid(),
                    TicketId = ticket.Id,
                    Author = ticket.UserId,
                    CreatedAt = DateTime.UtcNow,
                    Message = "Test message"
                };
            }
        }
    }
}