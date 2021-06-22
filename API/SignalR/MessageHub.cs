using System;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    public class MessageHub : Hub
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IMapper _mapper;
        private readonly IUserRepository _userRepository;
        private readonly IHubContext<PresenceHub> _presenceHub;
        private readonly PresenceTracker _tracker;

        public MessageHub(IMessageRepository messageRepository, IMapper mapper, IUserRepository userRepository, //kada budemo slali poruku mapiracemo je u Dto
        IHubContext<PresenceHub> presenceHub, //ako user nije konektovan konkretan hub ili konkretnu grupu hocemo da saljemo notification
        PresenceTracker tracker) //zato koristimo HubContext jer je dostupan svuda u aplikaciji i njega cemo iskoristiti u Message Hubu
                                            
        {
            _userRepository = userRepository;
            _presenceHub = presenceHub;
            _tracker = tracker;
            _mapper = mapper;
            _messageRepository = messageRepository;
        }

        //kreiracemo grupu za svakog usera, treba da definisemo groupName, bice kombinacija username i username, ali u alfabetical orderu
        //kada se user loguje na hub smestimo ga uvek u grupu sa userom sa kojim se dosuje ili sa kojim se vec dopisivao, uvek u istu grupu
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext(); //treba da dodjemo do usernamea drugog usera
            var otherUser = httpContext.Request.Query["user"].ToString(); //da saznamo na koji profil je trenutno logovan user kliknuo
            var groupName = GetGroupName(Context.User.GetUsername(), otherUser); //nije bitno da li je jedan user konketovan ili oba, 
                                                                                 //user ce uvek ici u ovu grupu
            //await AddToGroup(groupName);
            var group = await AddToGroup(groupName);
            await Clients.Group(groupName).SendAsync("UpdatedGroup", group);

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            //e sad kada user joinuje u grupu poruka se salje
            var message = await _messageRepository.GetMessageThread(Context.User.GetUsername(), otherUser);
            //await Clients.Group(groupName).SendAsync("ReceiveMessageThread", message); //kada je klijent konektovan saljemo message thread na oba usera iako jedan vec ima poruku
            await Clients.Caller.SendAsync("ReceiveMessageThread", message);
        }

        public override async Task OnDisconnectedAsync(System.Exception exception)
        {   
            //await RemoveFromMessageGroup();
            var group = await RemoveFromMessageGroup();
            await Clients.Group(group.Name).SendAsync("UpdatedGroup", group); //ako je grupa prazna signarR nista ne salje
            //signarR kada je user clan grupe, kada se diskonektuje automatski ga uklanja iz te grupe
            await base.OnDisconnectedAsync(exception);
        }
        //endpoint u Startup.cs

        //kopirana metoda iz MessageControlera
        public async Task SendMessage(CreateMessageDto createMessageDto)
        {
            var username = Context.User.GetUsername();

            if (username == createMessageDto.RecipientUsername.ToLower())
                throw new HubException("You cannot send messages to yourself");

            var sender = await _userRepository.GetUserByUsernameAsync(username);
            var recipient = await _userRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);

            if (recipient == null) throw new HubException("Not found user");

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipientUsername = recipient.UserName,
                Content = createMessageDto.Content
            };

            var groupName = GetGroupName(sender.UserName, recipient.UserName);

            var group = await _messageRepository.GetMessageGroup(groupName);

            if(group.Connections.Any(x => x.Username == recipient.UserName)) {
                message.DateRead = DateTime.UtcNow;
            } //ovde koristimo IHubContext<PresenceHub> ako nije u tom hubu ili u grupi
            else {
                var connections = await _tracker.GetConnectionsForUser(recipient.UserName); //uzimamo sve konekcije
                if(connections != null) { //znamo da je user online ali da niju konketovan u grupi
                    await _presenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived", //znamo da je konektovan pa mozemo presenceHub da koristimo
                        new {username = sender.UserName, knownAs = sender.KnownAs});
                }
            }

            _messageRepository.AddMessage(message);

            if (await _messageRepository.SaveAllAsync()) {

                await Clients.Group(groupName).SendAsync("NewMessage", _mapper.Map<MessageDto>(message));
            }
        }

        private async Task<Group> AddToGroup(/*HubCallerContext context,*/ string groupName) {//usrname i connectionID iz hubCallerContext
            var group = await _messageRepository.GetMessageGroup(groupName);
            var connection = new Connection(Context.ConnectionId, Context.User.GetUsername());

            if(group == null) {
                group = new Group(groupName);
                _messageRepository.AddGroup(group);
            }
            group.Connections.Add(connection);

            if(await _messageRepository.SaveAllAsync()) return group;
            //return await _messageRepository.SaveAllAsync();
            throw new HubException("Failed to join group");
        } 

        private async Task<Group> RemoveFromMessageGroup() {
            // var connection = await _messageRepository.GetConnection(Context.ConnectionId);
            var group = await _messageRepository.GetGroupForConnection(Context.ConnectionId);
            var connection = group.Connections.FirstOrDefault(x => x.ConnectionID == Context.ConnectionId);
            _messageRepository.RemoveConnection(connection);
            if(await _messageRepository.SaveAllAsync()) return group;

            throw new HubException("Failed to remove from group");
        }

        private string GetGroupName(string caller, string other)
        {
            var stringCompare = string.CompareOrdinal(caller, other) < 0; //ako je a < b onda vraca < 0
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }
    }
}