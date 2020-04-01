![AuthJanitor Logo](../master/docs/assets/img/AJLogoDark.png?raw=true)

![.NET Core](https://github.com/anthturner/AuthJanitor/workflows/.NET%20Core/badge.svg?branch=master)

Manage the lifecycle of your application secrets in Azure with ease. Migrate to more secure, auditable operations standards on your own terms. AuthJanitor supports varying levels of application secret security, based on your organization's security requirements.

## Concepts
### Task Queuing
The Key Management agent will pre-emptively queue a Rekeying Task when a Managed Secret is about to expire. The expectation
is that when using JIT administration (see below), this pre-emptive duration must be enough to allow an admin to manually
execute the rekeying prior to secret expiry.

### Administrator Signs Off Just-in-Time
Queued Rekeying Tasks using this strategy must be approved by an administrator. To provide the administrator with the proper knowledge to either approve or deny the Rekeying Task, all Providers are required to provide an Action Description and a scored list of Risks to help the administrator user understand the impact of their approval.

Once an administrator reviews this information, they can approve the actions, which are then executed in the administrator's user context using on-behalf-of access tokens. Approaching application secret rotation in this way allows for traceability of the actions performed back to the administrator approving them.

Using only Administrator-driven strategies is recommended, as it prevents the need for granting overly broad access to a service principal or managed identity.

### Administrator Caches Sign-Off Token
Similar to "Administrator Signs Off Just-in-Time" above, this approach uses an on-behalf-of token to act as the administrator for automated actions. However, this approach persists an access token/refresh token using a Secure Storage Provider (and optional Persistence Encryption). When the scheduled window arrives, the token is retrieved, used, and destroyed (if the rotation succeeded).

This method is less secure than signing off just-in-time, as a credential is saved for future use. However, it does allow for business continuity to dictate the secret rotation strategy, by setting availability windows. This could be used to only rotate secrets on weekends, for example.

### AuthJanitor Agent Rekeys Just-in-Time
When this strategy is used, the AuthJanitor Agent's identity context is used to perform the secret rotation. Immediately prior to the expiry of the Managed Secret, the Agent will execute the secret rotation.

### AuthJanitor Agent Rekeys on an Availability Schedule
Similar to the Just-in-Time strategy above, the AuthJanitor Agent's identity context is used to perform the secret rotation. However, this is done on a given schedule based on availability windows during which the secret rotation can occur.

### AuthJanitor Agent Rekeys When Prompted by an External Signal
When this strategy is used, the AuthJanitor Agent accepts HTTP requests from an external service. This request must contain the ObjectId of the Managed Secret, as well as what the external service believes the Managed Secret's nonce to be. If that nonce is valid, the Agent will return `0`. If it is invalid (i.e. the Managed Secret has been rotated, and a new nonce generated), the Agent will return `1`. The expectation is that the external service will reach out independently to a location where the secret material is stored to retrieve it. This may mean reloading an application to re-cache the value of a secret from Key Vault, for example.

### Secret Rotation Process
![Secret Rotation Process](../master/docs/assets/img/SecretRotationWorkflow.png?raw=true)

## Glossary of Terms
#### Provider
A module which implements some functionality, either to handle an **Application Lifecycle** or rekey a **Rekeyable Object**.

##### Provider Configuration
Configuration which the **Provider** consumes in order to access the **Application Lifecycle** or **Rekeyable Object**.

#### Application Lifecycle Provider
A **Provider** with the logic necessary to handle the lifecycle of application which consumes some information (key, secret, environment variable, connection string) to access a **Rekeyable Object**.

#### Rekeyable Object Provider
A **Provider** with the logic necessary to rekey a service or object. This might be a database, storage, or an encryption key.

#### Resource
A GUID-identified model which joins a **Provider** with its **Provider Configuration**. Resources also have a display name and description. A Resource can either wrap an **Application Lifecycle** _or_ a **Rekeyable Object** provider.

#### Managed Secret
A GUID-identified model which joins one or more **Resources** which make up one or more rekeyable objects and their corresponding application lifecycles. When using multiple rekeyable objects and/or lifecycles, a **User Hint** must be specified. When the rekeying is performed, the **User Hints** are matched between rekeyable objects and application lifecycles to identify where different secrets should be persisted. Managed Secrets also have a display name and description, as well as metadata on the validity period and rotation history.

#### Rekeying Task
A GUID-identified model to a **Managed Secret** which needs to be rekeyed. A Rekeying Task has a queued date and an expiry date; the
expiry date refers to the point in time where the key is rendered invalid. A Rekeying Task must be approved by an administrator or will be executed automatically by the AuthJanitor Agent.
