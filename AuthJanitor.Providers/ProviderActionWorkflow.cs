using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthJanitor.Providers
{
    public class ProviderActionWorkflow
    {
        private RekeyingAttemptLogger Logger { get; }
        public IEnumerable<IAuthJanitorProvider> Providers { get; }

        private IEnumerable<IRekeyableObjectProvider> RekeyableObjectProviders =>
            Providers.Where(p => p is IRekeyableObjectProvider)
                     .Cast<IRekeyableObjectProvider>();

        private IEnumerable<IApplicationLifecycleProvider> ApplicationLifecycleProviders =>
            Providers.Where(p => p is IApplicationLifecycleProvider)
                     .Cast<IApplicationLifecycleProvider>();

        private List<RegeneratedSecret> TemporarySecrets { get; } = new List<RegeneratedSecret>();
        private List<RegeneratedSecret> NewSecrets { get; } = new List<RegeneratedSecret>();

        public ProviderActionWorkflow(RekeyingAttemptLogger logger, IEnumerable<IAuthJanitorProvider> providers)
        {
            Logger = logger;
            Providers = providers;

            Logger.LogInformation("Created Provider Action Workflow for {0} ApplicationLifecycleProviders and {1} RekeyableObjectProviders",
                ApplicationLifecycleProviders.Count(),
                RekeyableObjectProviders.Count());

            Logger.LogInformation("The following providers will participate in this workflow: {0}",
                string.Join(", ", Providers.Select(p => p.GetType().Name)));
        }

        public async Task InvokeAsync(TimeSpan requestedValidPeriod)
        {
            Logger.LogInformation("Provider action workflow has been invoked.");

            // -----

            Logger.LogInformation("Performing sanity tests.");
            try
            {
                await Task.WhenAll(Providers.ToList().Select(p => p.Test()));
            }
            catch (Exception ex)
            {
                throw new Exception("Error running one or more sanity tests.", ex);
            }

            // -----

            Logger.LogInformation("Getting temporary secrets from {0} Rekeyable Object Lifecycle Providers...",
                RekeyableObjectProviders.Count());
            try
            {
                await Task.WhenAll(
                    RekeyableObjectProviders.Select(rkoProvider =>
                                 rkoProvider.GetSecretToUseDuringRekeying()
                                 .ContinueWith(t =>
                                 {
                                     if (t.Result != null)
                                         TemporarySecrets.Add(t.Result);
                                 })));
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving temporary secret(s) from Rekeyable Object Provider(s)", ex);
            }

            // -----

            Logger.LogInformation("Preparing Application Lifecycle Providers for rekeying...");
            try
            {
                await Task.WhenAll(
                    ApplicationLifecycleProviders.Select(alcProvider =>
                            alcProvider.BeforeRekeying(TemporarySecrets)));
            }
            catch (Exception ex)
            {
                throw new Exception("Error preparing Application Lifecycle Provider(s)", ex);
            }

            // -----

            Logger.LogInformation("Rekeying Rekeyable Object Providers...");
            try
            {
                await Task.WhenAll(RekeyableObjectProviders.Select(rop =>
                            rop.Rekey(requestedValidPeriod)
                            .ContinueWith(task => NewSecrets.Add(task.Result))));
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing Rekey on Rekeyable Object Provider(s)", ex);
            }

            // -----

            Logger.LogInformation("Committing {0} new managed secrets to {1} Application Lifecycle Providers...",
                NewSecrets.Count(),
                ApplicationLifecycleProviders.Count());
            try
            {
                await Task.WhenAll(ApplicationLifecycleProviders
                    .Select(alcProvider => alcProvider.CommitNewSecrets(NewSecrets)));
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing Commit on Application Lifecycle Provider(s)", ex);
            }

            // -----

            Logger.LogInformation("Completing post-rekey operations on Application Lifecycle Providers...");
            try
            {
                await Task.WhenAll(ApplicationLifecycleProviders
                    .Select(alp => alp.AfterRekeying()));
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing AfterRekeying on Application Lifecycle Provider(s)", ex);
            }

            // -----

            Logger.LogInformation("Completing finalizing operations on Rekeyable Object Providers...");
            try
            {
                await Task.WhenAll(RekeyableObjectProviders
                    .Select(rko => rko.OnConsumingApplicationSwapped()));
            }
            catch (Exception ex)
            {
                throw new Exception("Error executing OnConsumingApplicationSwapped on Rekeyable Object Provider(s)", ex);
            }
        }
    }
}