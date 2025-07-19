using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Wrap.Remastered.Schemas;

namespace Wrap.Remastered.Server.Managers;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public UserInfo Owner { get; set; } = null!;
    public List<UserInfo> Users { get; set; } = new();
    public int MaxUsers { get; set; } = 10;
}

public class RoomManager
{
    private readonly ConcurrentDictionary<int, Room> _rooms = new();
    private readonly ConcurrentDictionary<string, int> _userRoomMap = new(); // 用户ID->房间ID
    private int _nextRoomId = 1;

    public Room CreateRoom(string name, UserInfo owner, int maxUsers = 10)
    {
        // 检查用户是否已在其它房间
        if (_userRoomMap.ContainsKey(owner.UserId))
            throw new InvalidOperationException("用户已在其它房间，不能重复创建房间");
        var room = new Room
        {
            Id = _nextRoomId++,
            Name = name,
            Owner = owner,
            MaxUsers = maxUsers
        };
        room.Users.Add(owner);
        _rooms[room.Id] = room;
        _userRoomMap[owner.UserId] = room.Id;
        return room;
    }

    public Room? GetRoom(int id) => _rooms.TryGetValue(id, out var room) ? room : null;
    public IEnumerable<Room> GetAllRooms() => _rooms.Values;
    public bool RemoveRoom(int id)
    {
        if (_rooms.TryRemove(id, out var room))
        {
            foreach (var user in room.Users)
            {
                _userRoomMap.TryRemove(user.UserId, out _);
            }
            return true;
        }
        return false;
    }
    public bool AddUserToRoom(int roomId, UserInfo user)
    {
        // 检查用户是否已在其它房间
        if (_userRoomMap.ContainsKey(user.UserId))
            return false;
        if (_rooms.TryGetValue(roomId, out var room))
        {
            if (room.Users.Count < room.MaxUsers && !room.Users.Any(u => u.UserId == user.UserId))
            {
                room.Users.Add(user);
                _userRoomMap[user.UserId] = roomId;
                return true;
            }
        }
        return false;
    }
    public bool RemoveUserFromRoom(int roomId, string userId, Action<Room, string>? onOwnerChanged = null, Action<int, List<string>>? onRoomDismissed = null)
    {
        if (_rooms.TryGetValue(roomId, out var room))
        {
            var user = room.Users.FirstOrDefault(u => u.UserId == userId);
            if (user != null)
            {
                room.Users.Remove(user);
                _userRoomMap.TryRemove(userId, out _);
                // 房主离开，自动转让
                if (room.Owner.UserId == userId)
                {
                    if (room.Users.Count > 0)
                    {
                        var oldOwner = room.Owner.UserId;
                        room.Owner = room.Users[0];
                        onOwnerChanged?.Invoke(room, oldOwner);
                    }
                    else
                    {
                        // 无人则解散房间
                        var allUserIds = room.Users.Select(u => u.UserId).ToList();
                        _rooms.TryRemove(roomId, out _);
                        foreach (var uid in allUserIds)
                        {
                            _userRoomMap.TryRemove(uid, out _);
                        }
                        onRoomDismissed?.Invoke(roomId, allUserIds);
                    }
                }
                return true;
            }
        }
        return false;
    }
    // 可选：获取用户当前所在房间ID
    public int? GetUserRoomId(string userId)
    {
        return _userRoomMap.TryGetValue(userId, out var roomId) ? roomId : null;
    }
} 