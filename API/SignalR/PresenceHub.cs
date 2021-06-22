using System;
using System.Threading.Tasks;
using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    [Authorize] //SignalR ne moze da salje header kao http - koristi se query string tako da idemo u IdentityServiceExtensions za doradu
    public class PresenceHub : Hub
    {
        private readonly PresenceTracker _tracker;
        public PresenceHub(PresenceTracker tracker)
        {
            _tracker = tracker;
        }

        public async override Task OnConnectedAsync()
        {
            //kada se user konektuje, updejtujemo presence tracker i onda saljemo updejtovanu listu current usersa
            //svima koji su konektovani
            var isOnline = await _tracker.UserConnected(Context.User.GetUsername(), Context.ConnectionId);
            if(isOnline)
                await Clients.Others.SendAsync("UserIsOnline", Context.User.GetUsername()); //ovo vracamo samo ako je user online

            var currentUsers = await _tracker.GetOnlineUsers();
            await Clients.Caller.SendAsync("GetOnlineUsers", currentUsers); //vracamo listu svih konektovanih usera svima, to nije optimalno
                                                                            //zato umesto Clients.All.SendAsync ide Clients.Caller.SendAsync
        }

        public async override Task OnDisconnectedAsync(Exception exception)
        {
            var isOffline = await _tracker.UserDisconnected(Context.User.GetUsername(), Context.ConnectionId);
            if(isOffline)
                await Clients.Others.SendAsync("UserIsOffline", Context.User.GetUsername());

            //takodje kada se user diskonektuje mozemo ove dve linije da izbrisemo nema potreba da saljemo svima informaciju
            //dovoljno je samo da uploadujemo listu
            // var currentUsers = await _tracker.GetOnlineUsers();
            // await Clients.All.SendAsync("GetOnlineUsers", currentUsers);

            await base.OnDisconnectedAsync(exception);
            //sledece prikazujemo online usere - prvo idemo u presence.service i pravimo observable
        }
    }
}