namespace AuthorizationJanitor.Targets
{
    public class NamedResourceTarget : ITarget
    {
        public string ResourceGroup { get; set; }
        public string ResourceName { get; set; }
    }
}
