using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wrap.Remastered.Client;

public class UserProfile
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";

    private static string ProfilePath => Path.Combine(AppContext.BaseDirectory, "userprofile.json");

    public static UserProfile Load()
    {
        if (File.Exists(ProfilePath))
        {
            var json = File.ReadAllText(ProfilePath);
            return JsonSerializer.Deserialize(json, UserProfileJsonContext.Default.UserProfile) ?? new UserProfile();
        }
        return new UserProfile();
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, UserProfileJsonContext.Default.UserProfile);
        File.WriteAllText(ProfilePath, json);
    }
} 