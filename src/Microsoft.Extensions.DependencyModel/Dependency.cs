namespace Microsoft.Extensions.DependencyModel
{
    public struct Dependency
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