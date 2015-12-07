namespace Microsoft.Extensions.DependencyModel
{
    public class Target
    {
        public Target(string name, Library[] libraries, Runtime[] runtimes)
        {
            Name = name;
            Libraries = libraries;
            Runtimes = runtimes;
        }

        public string Name { get; }

        public Library[] Libraries { get; }
        public Runtime[] Runtimes { get; }
    }
}