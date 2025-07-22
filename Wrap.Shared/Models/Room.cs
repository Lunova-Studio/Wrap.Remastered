using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wrap.Shared.Models;

public record Room {
    public int Id { get; set; }
    public int MaxUsers { get; set; } = 10;
    public string OwnerUserId => Owner.UserId;
    public string Name { get; set; } = string.Empty;
    public UserInfo Owner { get; set; } = null!;
    public Dictionary<string, UserInfo> Users { get; set; } = [];
}