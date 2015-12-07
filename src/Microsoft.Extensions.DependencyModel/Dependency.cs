namespace Microsoft.Extensions.DependencyModel
{
    public class Dependency
    {
        public Dependency(string name, string version)
        {
            Name = name;
            Version = version;
        }

        public string Name { get; }
        public string Version { get; }
    }
}