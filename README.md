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

## Extensions
AuthJanitor Extensions are one of two types:

### RekeyableService
A service (or key) which can be rekeyed. This service is accessed by the ConsumingApplication and may or may not have interlocking
keys which can be rotated independently.

### ConsumingApplication
An application which consumes some information to connect to a RekeyableService. This could be a connection string, app setting,
environment variable, etc.