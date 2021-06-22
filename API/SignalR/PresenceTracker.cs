using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.SignalR
{
    //svaki put kada se user konektuje na hub bice mu dodeljen connectionID, 
    //tako da moze da se loguje sa razlicitih uredjaja, nista ga ne sprecava pa ce imati razlicite connectionID-eve
    //zato koristimo List<string>
    public class PresenceTracker
    {
        private static readonly Dictionary<string, List<string>> OnlineUsers = new Dictionary<string, List<string>>();

        //pravimo metode koje ce da dodaju usere u dictionary kada se loguju sa connectionID-evima i kada se diskonektuje
        public Task<bool> UserConnected(string username, string connectionID) { //bool naknadno dodat
            bool isOnline = false; //naknadno dodato
            //dictionary je deljan sa svima koji su logovani, a on nije siguran source
            lock (OnlineUsers) {//lokujemo dictionary dok ne obradi sta treba
                //da vidimo da li imamo vec u dictonary usera sa usernameom onda addujemo connectionId, 
                //u suprotnom kreiramo dictionary entry sa connectionIdem
                if(OnlineUsers.ContainsKey(username)) {
                    OnlineUsers[username].Add(connectionID); //dodajemo connectionID u listu
                }
                else {
                    OnlineUsers.Add(username, new List<string> {connectionID});
                    isOnline = true; //naknadno dodato
                }
            }
            // iza locka
            return Task.FromResult(isOnline); //Task.CompletedTask; 
        }

        public Task<bool> UserDisconnected(string username, string connectionID) {
            bool isOffline = false;
            lock(OnlineUsers) {
                if(!OnlineUsers.ContainsKey(username)) 
                    return Task.FromResult(isOffline); //Task.CompletedTask;

                OnlineUsers[username].Remove(connectionID);
                if(OnlineUsers[username].Count == 0) {
                    OnlineUsers.Remove(username);
                    isOffline = true;
                }
            }
            return Task.FromResult(isOffline); //Task.CompletedTask;
        }
        //metoda da dobijemo listu svih konektovanih usera
        public Task<string[]> GetOnlineUsers() {
            string[] onlineUsers;
            lock(OnlineUsers) { //k.Key je username, ne interesuje nas nista drugo
                onlineUsers = OnlineUsers.OrderBy(k => k.Key).Select(k => k.Key).ToArray(); //ako je user konektovan sa bilo kojim uredjajem onda cemo reci da je online
            }

            return Task.FromResult(onlineUsers);
            //posto je PresenceTracker u stvari servis koji ce biti dostupan u celoj aplikaciji tj. sherovan sa svim konekcijama na serveru
            //u ApplicationServiceExtension dodajemo singleton.
        }

        //lista svih konekcija konretnog usera
        public Task<List<string>> GetConnectionsForUser(string username) {
            List<string> connectionIDs;
            lock(OnlineUsers) { //GetValueOrDefault - ako user ne postoji u konekcijama onda vraca null kao default
                connectionIDs = OnlineUsers.GetValueOrDefault(username); //znaci ako postoji dictionary element sa usernamemom
                                                                        //vratice nam se lista sa connectionIDevima za tog usera
            }
            return Task.FromResult(connectionIDs);
        }
    }
}