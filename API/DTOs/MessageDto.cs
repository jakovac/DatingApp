using System;
using System.Text.Json.Serialization;

namespace API.DTOs
{
    public class MessageDto
    {
        public int ID { get; set; }
        public int SenderID { get; set; }
        public string SenderUsername { get; set; }
        public string SenderPhotoUrl { get; set; }
        public int RecipientID { get; set; }
        public string RecipientUsername { get; set; }
        public string RecipientPhotoUrl { get; set; }
        public string Content { get; set; }
        public DateTime? DateRead { get; set; }
        public DateTime MessageSent { get; set; }

        [JsonIgnore] //nece poslati ka klijentu ali cemo imati pristup njima u repositoriju
        public bool SenderDeleted { get; set; }
        [JsonIgnore]
        public bool RecipientDeleted { get; set; }
    }
}