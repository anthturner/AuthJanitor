![AuthJanitor Logo](../master/docs/assets/img/AJLogoDark.png?raw=true)

![.NET Core](https://github.com/anthturner/AuthJanitor/workflows/.NET%20Core/badge.svg?branch=master)

# AuthJanitor
Manage the lifecycle of application tokens, keys, and secrets in Azure

*NOTE: This is incomplete and just serves as a swiss-army knife of Extensions to rekey various things in Azure while 
prioritizing service availability*

## Concepts
### Task Queuing
The Key Management agent will pre-emptively queue a rekeying Task when a managed secret is about to expire. The expectation
is that when using JIT administration (see below), this pre-emptive duration must be enough to allow an admin to manually
execute the rekeying prior to secret expiry.

### Just-In-Time Administration
Queued rekeying tasks must be "signed off" by an administrator. To provide the administrator with the proper knowledge to either
approve or deny the rotation task, all Extensions must provide a GetDescription and a GetRisks method.

The GetDescription function should render a brief summarized explanation of the task which will be performed by the extension. 
The GetRisks function should return a scored list of risks and recommendations.

These two functions together are rendered to the administrator user as a "preview" of the tasks to be executed, as well as a brief
boolean test as to whether the admin's token can perform the necessary actions. This pre-test prevents partial rotation/breaking.

Once an administrator reviews this information, they can "sign off" on the actions, which are then executed in the admin's context
using OBO tokens. This restricts the amount of access the agent's service principal must have to the Azure subscription. Tasks
which cannot be executed as the administrator due to lack of privilege will be sent back to the queue.

### Latent Signature Administration
If JIT Administration is non-compliant with the expected business cases for the application (such as requiring rotations happen on
off-hours or weekends), an administrator can approve a Task for execution at a later time/date by providing an access token upfront
which is stored as securely as possible for the agent. This decreases security because it potentially exposes the access token for
an administrator and should only be used if required.

### Multi-Phase Rotation
When the Task is ready to be executed, the rotation occurs in several steps:
* The ConsumingApplication is given a chance to do pre-work prior to the rekeying action.
* The rekeying action is invoked against the RekeyableService
* The newly created key is stored as-appropriate for the ConsumingApplication
* The ConsumingApplication performs a swap or other action to "commit" configuration changes
* The rekeying action is informed the ConsumingApplication has completed its swap. This allows for the scrambling of unused keys.

## Glossary
#### Provider
A module which implements some functionality, either to handle an **Application Lifecycle** or rekey a **Rekeyable Object**.

##### Provider Configuration
Configuration which the **Provider** consumes in order to access the **Application Lifecycle** or **Rekeyable Object**.

#### Application Lifecycle Provider
A **Provider** with the logic necessary to handle the lifecycle of application which consumes some information (key, secret, environment variable,
connection string) to access a **Rekeyable Object**.

#### Rekeyable Object Provider
A **Provider** with the logic necessary to rekey a service or object. This might be a database, storage, or an encryption key.

#### Resource
A GUID-identified pair which joins a **Provider** with its **Provider Configuration**. Resources also have a display name and description.
A Resource can either be an **Application Lifecycle** _or_ a **Rekeyable Object**.

#### Managed Secret
A GUID-identified pair which joins an **Application Lifecycle Resource** with a **Rekeyable Object Resource**. Managed Secrets also have a
display name and description, as well as metadata on the validity period and rotation history.

#### Rekeying Task
A GUID-identified pointer to a **Managed Secret** which needs to be rekeyed. A Rekeying Task has a queued date and an expiry date; the
expiry date refers to the point in time where the key is rendered invalid. A Rekeying Task must be approved by an administrator.

## Sample Code
```csharp
public static async Task RotateKey()
{
    // Initialize DI container with your own LoggerFactory
    HelperMethods.InitializeServiceProvider(new LoggerFactory());

    // Get a RekeyableObjectProvider (this one is for Azure Storage keys)
    var rekeyableProvider = HelperMethods.ServiceProvider.GetService(
        typeof(Providers.Storage.StorageAccountRekeyableObjectProvider)) as Providers.Storage.StorageAccountRekeyableObjectProvider;

    // Configure the RekeyableObjectProvider
    rekeyableProvider.Configuration = new Providers.Storage.StorageAccountKeyConfiguration()
    {
        KeyType = Providers.Storage.StorageAccountKeyConfiguration.StorageKeyTypes.Key1,
        ResourceGroup = "resource_group",
        ResourceName = "resource_name",
        SkipScramblingOtherKey = false
    };

    // Get an ApplicationLifecycleProvider (this one is for an Azure WebApp, consuming from AppSettings)
    var appProvider = HelperMethods.ServiceProvider.GetService(
        typeof(Providers.AppServices.WebApps.AppSettingsWebAppApplicationLifecycleProvider)) as Providers.AppServices.WebApps.AppSettingsWebAppApplicationLifecycleProvider;
    
    // Configure the ApplicationLifecycleProvider
    appProvider.Configuration = new Providers.AppServices.AppSettingConfiguration()
    {
        ResourceGroup = "resource_group",
        ResourceName = "resource_name",
        SettingName = "storage_key",
        SourceSlot = "production",
        TemporarySlot = "temporary",
        DestinationSlot = "production"
    };

    // Execute the rekeying workflow.
    // This will:
    // - Run sanity tests on all Providers
    // - Prep all ApplicationLifecycleProviders
    // - Rekey all RekeyableObjectProviders
    // - Commit generated keys to ApplicationLifecycleProviders
    // - Run post-commit activities on all ApplicationLifecycleProviders
    await HelperMethods.RunRekeyingWorkflow(TimeSpan.FromDays(7), rekeyableProvider, appProvider);
}
```
