using System;
using System.Collections.Generic;

namespace Contracts
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        // public ICollection<Ticket> Tickets { get; set; }
    }

    public class Ticket
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<Comment> Comments { get; set; }
    }

    public class Comment
    {
        public Guid Id { get; set; }
        public Guid TicketId { get; set; }
        public Guid Author { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}