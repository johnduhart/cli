namespace Microsoft.Extensions.DependencyModel
{
    public class Runtime
    {
        public Runtime(string runtimeId, Library[] runtimeLibraries)
        {
            RuntimeId = runtimeId;
            Libraries = runtimeLibraries;
        }

        public string RuntimeId { get; }
        public Library[] Libraries { get; }
    }
}