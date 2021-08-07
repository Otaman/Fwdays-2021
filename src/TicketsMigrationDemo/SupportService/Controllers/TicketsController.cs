using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupportService.Database;

namespace SupportService.Controllers
{
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private readonly TicketsContext _context;

        public TicketsController(TicketsContext context)
        {
            _context = context;
        }

        [HttpGet("/users/{userId}")]
        public async Task<ActionResult<User>> GetUser(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user != null) return user;
            return NotFound();
        }

        [HttpGet("/users/{userId}/tickets")]
        public async Task<IEnumerable<Ticket>> GetTickets(Guid userId) =>
            await _context.Tickets.Where(x => x.UserId == userId).ToArrayAsync();

        [HttpGet("/tickets/{ticketId}/comments")]
        public async Task<IEnumerable<Comment>> GetComments(Guid ticketId) =>
            await _context.Comments.Where(x => x.TicketId == ticketId).ToArrayAsync();
    }
}