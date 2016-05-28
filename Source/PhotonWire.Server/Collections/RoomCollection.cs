using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace PhotonWire.Server.Collections
{
    public enum JoinRoomReason
    {
        Joined, JoinedAndJustFull, NotJoinedAlreadyFull, NotJoinedRoomNotFound, NotJoinedAlreadyJoined
    }

    public enum RemoveRoomMemberReason
    {
        RemovedMember, RemovedAndRoomDeleted, NotRemovedRoomNotFound, NotRemovedMemberNotFound
    }

    /// <summary>
    /// Base class for RoomCollection.
    /// </summary>
    public abstract class RoomBase<TMemberKey, TMember>
        where TMember : RoomMemberBase
    {
        internal string InternalRoomIdentifier { get; }

        /// <summary>
        /// Dictionary of Members Key's equality comparer.
        /// </summary>
        protected virtual IEqualityComparer<TMemberKey> MembersKeyEqualityComparer { get; } = EqualityComparer<TMemberKey>.Default;

        /// <summary>
        /// Dictionary of Members Value's equality comparer.
        /// </summary>
        protected virtual IEqualityComparer<TMember> MembersValueEqualityComparer { get; } = EqualityComparer<TMember>.Default;

        public abstract void OnMemberDisconnected(TMember disconnectedMember);

        /// <summary>
        /// Members are immutable, modified only in RoomCollection.
        /// </summary>
        public IImmutableDictionary<TMemberKey, TMember> Members { get; internal set; }

        /// <summary>
        /// Peers of room member.
        /// </summary>
        public IEnumerable<IPhotonWirePeer> MemberPeers => Members.Select(x => x.Value.Peer);

        public RoomBase()
        {
            Members = ImmutableDictionary.Create(MembersKeyEqualityComparer, MembersValueEqualityComparer);
            InternalRoomIdentifier = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Bass class for RoomCollection's member.
    /// </summary>
    public abstract class RoomMemberBase
    {
        public IPhotonWirePeer Peer { get; }

        public RoomMemberBase(IPhotonWirePeer peer)
        {
            this.Peer = peer;
        }
    }

    /// <summary>
    /// Threadsafe coordinater, room and members.
    /// </summary>
    public class RoomCollection<TRoomKey, TRoom, TMemberKey, TMember>
        where TRoom : RoomBase<TMemberKey, TMember>
        where TMember : RoomMemberBase
    {
        object gate = new object();

        readonly Dictionary<TRoomKey, TRoom> rooms;
        readonly bool deleteIfRoomMemberIsEmpty;

        public RoomCollection()
            : this(EqualityComparer<TRoomKey>.Default)
        {

        }

        public RoomCollection(
            Func<TRoomKey, object> roomKeyEqualitySelector)
            : this(ComparerHelper.Create(roomKeyEqualitySelector))
        {
        }

        public RoomCollection(
            IEqualityComparer<TRoomKey> roomKeyEqualityComparer)
            : this(roomKeyEqualityComparer, true)
        {
        }

        public RoomCollection(
            IEqualityComparer<TRoomKey> roomKeyEqualityComparer, bool deleteIfRoomMemberIsEmpty)
        {
            this.rooms = new Dictionary<TRoomKey, TRoom>(roomKeyEqualityComparer);
            this.deleteIfRoomMemberIsEmpty = deleteIfRoomMemberIsEmpty;
        }

        IDisposable RegisterDisconnect(TRoomKey key, TMemberKey targetMemberKey, TMember targetMember)
        {
            return targetMember.Peer.RegisterDisconnectAction((_, __) =>
            {
                TRoom room;
                var reason = this.RemoveMember(key, targetMemberKey, null, out room);
                if (reason == RemoveRoomMemberReason.RemovedMember)
                {
                    room.OnMemberDisconnected(targetMember);
                }
            });
        }

        /// <summary>
        /// Create new room with member.
        /// </summary>
        public bool CreateRoom(TRoomKey roomKey, TRoom newRoom, TMemberKey memberKey, TMember newMember)
        {
            newMember.Peer.Items[newRoom.InternalRoomIdentifier] = RegisterDisconnect(roomKey, memberKey, newMember);

            lock (gate)
            {
                if (rooms.ContainsKey(roomKey)) return false;

                rooms.Add(roomKey, newRoom);
                newRoom.Members = newRoom.Members.Add(memberKey, newMember);

                return true;
            }
        }

        /// <summary>
        /// Remove room. If can removed return true otherwise else.
        /// </summary>
        public bool RemoveRoom(TRoomKey roomKey, out TRoom room)
        {
            lock (gate)
            {
                if (!rooms.TryGetValue(roomKey, out room)) return false;

                rooms.Remove(roomKey);
                return true;
            }
        }

        /// <summary>
        /// Get room. If not found return null.
        /// </summary>
        public TRoom GetRoom(TRoomKey roomKey)
        {
            lock (gate)
            {
                TRoom room;
                return rooms.TryGetValue(roomKey, out room)
                    ? room
                    : null;
            }
        }

        /// <summary>
        /// Get all rooms.
        /// </summary>
        public TRoom[] GetAllRoom()
        {
            lock (gate)
            {
                return rooms.Select(x => x.Value).ToArray();
            }
        }

        /// <summary>
        /// Join new room. If JoinRoomReason.Joined* is succeeed otherwise failed.
        /// </summary>
        /// <param name="sideEffectBeforeJoin">Thread safety side effects in the lock for set playerNo etc. Action is in the lock so should be lightweight.</param>
        public JoinRoomReason JoinRoom(TRoomKey roomKey, TMemberKey memberKey, TMember member, int? roomMaxMemberCount, Action<TRoom, TMember> sideEffectBeforeJoin, out TRoom room)
        {
            room = default(TRoom);
            lock (gate)
            {
                if (!rooms.TryGetValue(roomKey, out room))
                {
                    return JoinRoomReason.NotJoinedRoomNotFound;
                }

                if (roomMaxMemberCount != null)
                {
                    if (room.Members.Count >= roomMaxMemberCount)
                    {
                        return JoinRoomReason.NotJoinedAlreadyFull;
                    }
                }

                var before = room.Members;
                if (sideEffectBeforeJoin != null)
                {
                    sideEffectBeforeJoin(room, member); // threadsafe(in lock) side effect. for set playerNo etc...
                }

                member.Peer.Items[room.InternalRoomIdentifier] = RegisterDisconnect(roomKey, memberKey, member); // when disconnected leave

                room.Members = room.Members.Add(memberKey, member);
                if (before == room.Members) return JoinRoomReason.NotJoinedAlreadyJoined;

                if (roomMaxMemberCount != null && room.Members.Count == roomMaxMemberCount)
                {
                    return JoinRoomReason.JoinedAndJustFull;
                }
                else
                {
                    return JoinRoomReason.Joined;
                }
            }
        }

        /// <summary>
        /// Remove member from the room. If RemoveRoomMemberReason.Removed* is succeeed otherwise failed.
        /// </summary>
        /// <param name="sideEffectAfterRemove">Thread safety side effects in the lock for re-set playerNo etc. Action is in the lock so should be lightweight.</param>
        public RemoveRoomMemberReason RemoveMember(TRoomKey roomKey, TMemberKey memberKey, Action<IImmutableDictionary<TMemberKey, TMember>> sideEffectAfterRemove, out TRoom room)
        {
            room = default(TRoom);
            lock (gate)
            {
                if (!rooms.TryGetValue(roomKey, out room)) return RemoveRoomMemberReason.NotRemovedRoomNotFound;

                TMember member;
                if (!room.Members.TryGetValue(memberKey, out member)) return RemoveRoomMemberReason.NotRemovedMemberNotFound;

                var before = room.Members;
                room.Members = room.Members.Remove(memberKey);

                if (before == room.Members) return RemoveRoomMemberReason.NotRemovedMemberNotFound;

                (member.Peer.Items[room.InternalRoomIdentifier] as IDisposable)?.Dispose(); // register disconnect unsubscribe

                if (sideEffectAfterRemove != null)
                {
                    sideEffectAfterRemove(room.Members); // threadsafe(in lock) side effect. for set playerNo etc...
                }

                if (room.Members.Count == 0 && deleteIfRoomMemberIsEmpty)
                {
                    rooms.Remove(roomKey);
                    return RemoveRoomMemberReason.RemovedAndRoomDeleted;
                }
                else
                {
                    return RemoveRoomMemberReason.RemovedMember;
                }
            }
        }

        /// <summary>
        /// Do side-effect action in key lock, so action should be lightweight.
        /// </summary>
        public bool ModifyRoom(TRoomKey key, Action<TRoom> sideEffect, out TRoom room)
        {
            lock (gate)
            {
                if (!rooms.TryGetValue(key, out room)) return false;

                sideEffect(room);

                return true;
            }
        }

        /// <summary>
        /// Replace Members in key lock.
        /// </summary>
        public bool ReplaceMembers(TRoomKey key, Func<TRoom, IImmutableDictionary<TMemberKey, TMember>> newMembersSelector, out TRoom room)
        {
            lock (gate)
            {
                if (!rooms.TryGetValue(key, out room)) return false;

                var newMembers = newMembersSelector(room);
                room.Members = newMembers;

                return true;
            }
        }
    }
}