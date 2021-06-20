using System;

namespace API.Entities
{
    public class Message
    {
        public int ID { get; set; }
        public int SenderID { get; set; }
        public string SenderUsername { get; set; }
        public AppUser Sender { get; set; }
        public int RecipientID { get; set; }
        public string RecipientUsername { get; set; }
        public AppUser Recipient { get; set; }
        public string Content { get; set; }
        public DateTime? DateRead { get; set; }
        public DateTime MessageSent { get; set; } = DateTime.Now;
        public bool SenderDeleted { get; set; }
        public bool RecipientDeleted { get; set; }
    }
}