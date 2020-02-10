namespace AuthorizationJanitor.Targets
{
    public class KeyVaultTarget : NamedResourceTarget
    {
        public string VaultName { get; set; }

        public string KeyOrSecretName { get; set; }
    }
}
