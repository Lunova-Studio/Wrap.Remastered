using System.Text.Json.Serialization;

namespace Wrap.Remastered.Client;

[JsonSerializable(typeof(UserProfile))]
public partial class UserProfileJsonContext : JsonSerializerContext
{
} 