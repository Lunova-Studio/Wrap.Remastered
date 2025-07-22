namespace Wrap.Shared;

public sealed class NamespacedKey {
    public static NamespacedKey Empty { get; } = new NamespacedKey(string.Empty, string.Empty);

    public string Namespace { get; set; }
    public string Key { get; set; }

    public NamespacedKey(string @namespace, string key) {
        Namespace = @namespace;
        Key = key;
    }

    public override string ToString() {
        return $"{Namespace}:{Key}";
    }

    public static NamespacedKey Parse(string namespacedKey) {
        if (string.IsNullOrEmpty(namespacedKey))
            throw new ArgumentException("Namespaced key cannot be null or empty.", nameof(namespacedKey));

        var parts = namespacedKey.Split(':');
        if (parts.Length != 2)
            throw new FormatException("Invalid namespaced key format. Expected format is 'namespace:key'.");

        return new NamespacedKey(parts[0], parts[1]);
    }
}