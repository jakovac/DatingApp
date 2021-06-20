namespace API.Helpers
{
    public class MessageParams : PaginationParams
    {
        public string Username { get; set; } //logovan user
        public string Container { get; set; } = "Unread"; //defaultno neprocitana poruka
    }
}