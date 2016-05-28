using System;
using System.Collections.Immutable;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PhotonWire.Server;
using PhotonWire.Server.Collections;

// SampleCase, Chat.

namespace PhotonWire.Sample.ServerApp.GameServer.Hubs
{
    public class ChatRoom : RoomBase<int, ChatMember>
    {
        public string RoomId { get; set; }
        public string RoomName { get; set; }

        public override void OnMemberDisconnected(ChatMember disconnectedMember)
        {
            // broadcast leaveUser.
            var hub = PeerManager.GetHubContext<ChatHub, IChatClient>();
            hub.Clients.Targets(this.MemberPeers)
                .LeaveUser(disconnectedMember.Name);
        }
    }

    public class ChatMember : RoomMemberBase
    {
        public int MemberNo { get; set; }
        public string Name { get; set; }

        public ChatMember(IPhotonWirePeer peer)
            : base(peer)
        {

        }
    }

    public class ChatHubException : CustomErrorException
    {
        public ChatHubException(string errorMessage)
            : base(1, errorMessage)
        {
        }
    }

    public interface IChatClient
    {
        [Operation(0)]
        void ReceiveMessage(string userName, string message);
        [Operation(1)]
        void JoinUser(string userName);
        [Operation(2)]
        void LeaveUser(string userName);
    }

    // Server to Client = 1:1 Model Chat Sample
    [Hub(9)]
    public class ChatHub : Hub<IChatClient>
    {
        const int MaxRoomMemeberCount = 25;

        // InMemory Storage
        static RoomCollection<string, ChatRoom, int, ChatMember> currentRooms = new RoomCollection<string, ChatRoom, int, ChatMember>();

        // Client Proxy
        IChatClient InRoomMembers(ChatRoom room) => this.Clients.Targets(room.MemberPeers);

        // Operations

        [Operation(0)]
        public string CreateRoom(string roomName, string userName)
        {
            var roomId = Guid.NewGuid().ToString();
            var room = new ChatRoom { RoomId = roomId, RoomName = roomName };
            var newMember = new ChatMember(this.Context.Peer) { MemberNo = 1, Name = userName };

            if (!currentRooms.CreateRoom(roomId, room, newMember.Peer.ConnectionId, newMember))
            {
                throw new ChatHubException("room not found");
            }

            return roomId;
        }

        [Operation(1)]
        public string[] GetRooms()
        {
            return currentRooms.GetAllRoom().Select(x => x.RoomId).ToArray();
        }

        [Operation(2)]
        public string[] GetRoomMembers(string roomId)
        {
            return currentRooms.GetRoom(roomId)?.Members.Select(x => x.Value.Name).ToArray() ?? new string[0];
        }

        [Operation(3)]
        public void PublishMessage(string roomId, string message)
        {
            var room = currentRooms.GetRoom(roomId);
            if (room == null) return;

            var member = room.Members.GetValueOrDefault(Context.Peer.ConnectionId);
            if (member == null) return;

            InRoomMembers(room).ReceiveMessage(member.Name, message);
        }

        [Operation(4)]
        public void JoinRoom(string roomId, string userName)
        {
            var member = new ChatMember(Context.Peer) { Name = userName };

            ChatRoom room;
            currentRooms.JoinRoom(roomId, Context.Peer.ConnectionId, member, MaxRoomMemeberCount, (inRoom, newMember) =>
           {
               // set blank no
               var seq = 1;
               foreach (var item in inRoom.Members.OrderBy(x => x.Value.MemberNo))
               {
                   if (item.Value.MemberNo != seq) break;
                   seq++;
               }
               newMember.MemberNo = seq;
           }, out room);

            InRoomMembers(room).JoinUser(member.Name);
        }

        [Operation(5)]
        public void LeaveRoom(string roomId)
        {
            var room = currentRooms.GetRoom(roomId);
            if (room == null) return;

            var member = room.Members.GetValueOrDefault(Context.Peer.ConnectionId);
            if (member == null) return;

            var reason = currentRooms.RemoveMember(roomId, Context.Peer.ConnectionId, null, out room);

            if (reason == RemoveRoomMemberReason.RemovedMember)
            {
                InRoomMembers(room).LeaveUser(member.Name);
            }
        }
    }
}