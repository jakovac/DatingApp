namespace API.Entities
{
    public class Connection
    {
        public Connection()
        {
        }

        public Connection(string connectionID, string username)
        {
            ConnectionID = connectionID;
            Username = username;
        }

        public string ConnectionID { get; set; }
        public string Username { get; set; }
    }
}