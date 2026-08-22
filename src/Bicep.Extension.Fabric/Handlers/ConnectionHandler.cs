using CoreModels = Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class ConnectionHandler : FabricResourceHandlerBase<Connection, ConnectionIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            CoreModels.Connection result;

            if (request.Properties.Id is { } idValue)
            {
                var connectionId = ParseId(idValue, "id");
                result = (await client.Core.Connections.UpdateConnectionAsync(
                    connectionId,
                    ToSdkUpdateRequest(request.Properties),
                    cancellationToken)).Value;
            }
            else
            {
                result = (await client.Core.Connections.CreateConnectionAsync(
                    ToSdkCreateRequest(request.Properties),
                    cancellationToken)).Value;
            }

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var result = (await client.Core.Connections.GetConnectionAsync(RequireId(request.Identifiers.Id), cancellationToken)).Value;
            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            await client.Core.Connections.DeleteConnectionAsync(RequireId(request.Identifiers.Id), cancellationToken);
            return GetResponse(request, properties: null);
        });

    protected override ConnectionIdentifiers GetIdentifiers(Connection properties)
        => new() { Id = properties.Id };

    private static CoreModels.CreateConnectionRequest ToSdkCreateRequest(Connection properties)
    {
        var connectionDetails = ToSdkCreateConnectionDetails(properties.ConnectionDetails);
        var credentialDetails = ToSdkCreateCredentialDetails(properties.CredentialDetails);

        CoreModels.CreateConnectionRequest request = properties.ConnectivityType switch
        {
            ConnectionConnectivityType.ShareableCloud => new CoreModels.CreateCloudConnectionRequest(properties.DisplayName, connectionDetails, credentialDetails)
            {
                AllowConnectionUsageInGateway = properties.AllowConnectionUsageInGateway,
                AllowUsageInUserControlledCode = properties.AllowUsageInUserControlledCode,
            },
            ConnectionConnectivityType.VirtualNetworkGateway => new CoreModels.CreateVirtualNetworkGatewayConnectionRequest(
                properties.DisplayName,
                connectionDetails,
                ParseId(properties.GatewayId ?? throw new ResourceErrorException("MissingProperty", "gatewayId is required when connectivityType is VirtualNetworkGateway.", "gatewayId"), "gatewayId"),
                credentialDetails),
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported connectivity type '{properties.ConnectivityType}'.", "connectivityType"),
        };

        request.PrivacyLevel = properties.PrivacyLevel is { } privacyLevel ? ToSdkPrivacyLevel(privacyLevel) : null;
        return request;
    }

    private static CoreModels.UpdateConnectionRequest ToSdkUpdateRequest(Connection properties)
    {
        var credentialDetails = ToSdkUpdateCredentialDetails(properties.CredentialDetails);

        CoreModels.UpdateConnectionRequest request = properties.ConnectivityType switch
        {
            ConnectionConnectivityType.ShareableCloud => new CoreModels.UpdateShareableCloudConnectionRequest
            {
                DisplayName = properties.DisplayName,
                CredentialDetails = credentialDetails,
                AllowConnectionUsageInGateway = properties.AllowConnectionUsageInGateway,
            },
            ConnectionConnectivityType.VirtualNetworkGateway => new CoreModels.UpdateVirtualNetworkGatewayConnectionRequest
            {
                DisplayName = properties.DisplayName,
                CredentialDetails = credentialDetails,
            },
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported connectivity type '{properties.ConnectivityType}'.", "connectivityType"),
        };

        request.PrivacyLevel = properties.PrivacyLevel is { } privacyLevel ? ToSdkPrivacyLevel(privacyLevel) : null;
        return request;
    }

    private static CoreModels.CreateConnectionDetails ToSdkCreateConnectionDetails(ConnectionDetailsModel details)
    {
        var parameters = (details.Parameters ?? []).Select(ToSdkConnectionDetailsParameter);
        return new CoreModels.CreateConnectionDetails(details.Type, details.CreationMethod, parameters);
    }

    private static CoreModels.ConnectionDetailsParameter ToSdkConnectionDetailsParameter(ConnectionDetailsParameter parameter)
    {
        var target = $"connectionDetails.parameters['{parameter.Name}'].value";

        return (parameter.DataType ?? ConnectionParameterDataType.Text) switch
        {
            ConnectionParameterDataType.Text => new CoreModels.ConnectionDetailsTextParameter(parameter.Name, parameter.Value),
            ConnectionParameterDataType.Number => new CoreModels.ConnectionDetailsNumberParameter(parameter.Name, ParseSingle(parameter.Value, target)),
            ConnectionParameterDataType.Boolean => new CoreModels.ConnectionDetailsBooleanParameter(parameter.Name, ParseBoolean(parameter.Value, target)),
            ConnectionParameterDataType.Date => new CoreModels.ConnectionDetailsDateParameter(parameter.Name, ParseTimestamp(parameter.Value, target)),
            ConnectionParameterDataType.DateTime => new CoreModels.ConnectionDetailsDateTimeParameter(parameter.Name, ParseTimestamp(parameter.Value, target)),
            ConnectionParameterDataType.DateTimeZone => new CoreModels.ConnectionDetailsDateTimeZoneParameter(parameter.Name, parameter.Value),
            ConnectionParameterDataType.Duration => new CoreModels.ConnectionDetailsDurationParameter(parameter.Name, parameter.Value),
            ConnectionParameterDataType.Time => new CoreModels.ConnectionDetailsTimeParameter(parameter.Name, ParseTime(parameter.Value, target)),
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported parameter data type '{parameter.DataType}'.", target),
        };
    }

    private static float ParseSingle(string value, string target)
        => float.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new ResourceErrorException("InvalidNumber", $"'{value}' is not a valid number.", target);

    private static bool ParseBoolean(string value, string target)
        => bool.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidBoolean", $"'{value}' is not a valid boolean.", target);

    private static TimeSpan ParseTime(string value, string target)
        => TimeSpan.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new ResourceErrorException("InvalidTime", $"'{value}' is not a valid time.", target);

    private static CoreModels.CreateCredentialDetails ToSdkCreateCredentialDetails(ConnectionCredentialDetails details)
        => new(ToSdkCredentials(details))
        {
            ConnectionEncryption = details.ConnectionEncryption is { } encryption ? ToSdkEncryption(encryption) : null,
            SingleSignOnType = details.SingleSignOnType is { } sso ? ToSdkSingleSignOnType(sso) : null,
            SkipTestConnection = details.SkipTestConnection,
        };

    private static CoreModels.UpdateCredentialDetails ToSdkUpdateCredentialDetails(ConnectionCredentialDetails details)
        => new()
        {
            Credentials = ToSdkCredentials(details),
            ConnectionEncryption = details.ConnectionEncryption is { } encryption ? ToSdkEncryption(encryption) : null,
            SingleSignOnType = details.SingleSignOnType is { } sso ? ToSdkSingleSignOnType(sso) : null,
            SkipTestConnection = details.SkipTestConnection,
        };

    private static CoreModels.Credentials ToSdkCredentials(ConnectionCredentialDetails details)
        => details.CredentialType switch
        {
            ConnectionCredentialType.Anonymous => new CoreModels.AnonymousCredentials(),
            ConnectionCredentialType.Basic => ToSdkBasicCredentials(details.Basic
                ?? throw new ResourceErrorException("MissingProperty", "credentialDetails.basic is required when credentialType is Basic.", "credentialDetails.basic")),
            ConnectionCredentialType.Key => ToSdkKeyCredentials(details.Key
                ?? throw new ResourceErrorException("MissingProperty", "credentialDetails.key is required when credentialType is Key.", "credentialDetails.key")),
            ConnectionCredentialType.KeyPair => ToSdkKeyPairCredentials(details.KeyPair
                ?? throw new ResourceErrorException("MissingProperty", "credentialDetails.keyPair is required when credentialType is KeyPair.", "credentialDetails.keyPair")),
            ConnectionCredentialType.ServicePrincipal => ToSdkServicePrincipalCredentials(details.ServicePrincipal
                ?? throw new ResourceErrorException("MissingProperty", "credentialDetails.servicePrincipal is required when credentialType is ServicePrincipal.", "credentialDetails.servicePrincipal")),
            ConnectionCredentialType.SharedAccessSignature => ToSdkSharedAccessSignatureCredentials(details.SharedAccessSignature
                ?? throw new ResourceErrorException("MissingProperty", "credentialDetails.sharedAccessSignature is required when credentialType is SharedAccessSignature.", "credentialDetails.sharedAccessSignature")),
            ConnectionCredentialType.Windows => ToSdkWindowsCredentials(details.Windows
                ?? throw new ResourceErrorException("MissingProperty", "credentialDetails.windows is required when credentialType is Windows.", "credentialDetails.windows")),
            ConnectionCredentialType.WindowsWithoutImpersonation => new CoreModels.WindowsWithoutImpersonationCredentials(),
            ConnectionCredentialType.WorkspaceIdentity => new CoreModels.WorkspaceIdentityCredentials(),
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported credential type '{details.CredentialType}'.", "credentialDetails.credentialType"),
        };

    private static CoreModels.BasicCredentials ToSdkBasicCredentials(ConnectionBasicCredentials credentials)
        => new(credentials.Username) { Password = credentials.Password, PasswordReference = ToSdkSecretReference(credentials.PasswordReference) };

    private static CoreModels.KeyCredentials ToSdkKeyCredentials(ConnectionKeyCredentials credentials)
        => new() { Key = credentials.Key, KeyReference = ToSdkSecretReference(credentials.KeyReference) };

    private static CoreModels.KeyPairCredentials ToSdkKeyPairCredentials(ConnectionKeyPairCredentials credentials)
        => new(credentials.Identifier, credentials.PrivateKey) { Passphrase = credentials.Passphrase };

    private static CoreModels.ServicePrincipalCredentials ToSdkServicePrincipalCredentials(ConnectionServicePrincipalCredentials credentials)
        => new(ParseId(credentials.TenantId, "credentialDetails.servicePrincipal.tenantId"), ParseId(credentials.ClientId, "credentialDetails.servicePrincipal.clientId"))
        {
            ServicePrincipalSecret = credentials.ClientSecret,
            ServicePrincipalSecretReference = ToSdkSecretReference(credentials.ClientSecretReference),
        };

    private static CoreModels.SharedAccessSignatureCredentials ToSdkSharedAccessSignatureCredentials(ConnectionSharedAccessSignatureCredentials credentials)
        => new() { Token = credentials.Token, TokenReference = ToSdkSecretReference(credentials.TokenReference) };

    private static CoreModels.WindowsCredentials ToSdkWindowsCredentials(ConnectionWindowsCredentials credentials)
        => new(credentials.Username, credentials.Password);

    private static CoreModels.KeyVaultSecretReference? ToSdkSecretReference(ConnectionKeyVaultSecretReference? reference)
        => reference is null
            ? null
            : new CoreModels.KeyVaultSecretReference(ParseId(reference.ConnectionId, "connectionId"), reference.SecretName) { Version = reference.Version };

    private static CoreModels.PrivacyLevel ToSdkPrivacyLevel(ConnectionPrivacyLevel value)
        => value switch
        {
            ConnectionPrivacyLevel.None => CoreModels.PrivacyLevel.None,
            ConnectionPrivacyLevel.Organizational => CoreModels.PrivacyLevel.Organizational,
            ConnectionPrivacyLevel.Private => CoreModels.PrivacyLevel.Private,
            ConnectionPrivacyLevel.Public => CoreModels.PrivacyLevel.Public,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported privacy level '{value}'.", "privacyLevel"),
        };

    private static ConnectionPrivacyLevel ToModelPrivacyLevel(CoreModels.PrivacyLevel value)
        => value.ToString() switch
        {
            nameof(ConnectionPrivacyLevel.None) => ConnectionPrivacyLevel.None,
            nameof(ConnectionPrivacyLevel.Organizational) => ConnectionPrivacyLevel.Organizational,
            nameof(ConnectionPrivacyLevel.Private) => ConnectionPrivacyLevel.Private,
            nameof(ConnectionPrivacyLevel.Public) => ConnectionPrivacyLevel.Public,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported privacy level '{other}' returned by the Fabric API."),
        };

    private static CoreModels.ConnectionEncryption ToSdkEncryption(ConnectionEncryptionType value)
        => value switch
        {
            ConnectionEncryptionType.Any => CoreModels.ConnectionEncryption.Any,
            ConnectionEncryptionType.Encrypted => CoreModels.ConnectionEncryption.Encrypted,
            ConnectionEncryptionType.NotEncrypted => CoreModels.ConnectionEncryption.NotEncrypted,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported connection encryption '{value}'.", "credentialDetails.connectionEncryption"),
        };

    private static ConnectionEncryptionType ToModelEncryption(CoreModels.ConnectionEncryption value)
        => value.ToString() switch
        {
            nameof(ConnectionEncryptionType.Any) => ConnectionEncryptionType.Any,
            nameof(ConnectionEncryptionType.Encrypted) => ConnectionEncryptionType.Encrypted,
            nameof(ConnectionEncryptionType.NotEncrypted) => ConnectionEncryptionType.NotEncrypted,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported connection encryption '{other}' returned by the Fabric API."),
        };

    private static CoreModels.SingleSignOnType ToSdkSingleSignOnType(ConnectionSingleSignOnType value)
        => value switch
        {
            ConnectionSingleSignOnType.None => CoreModels.SingleSignOnType.None,
            ConnectionSingleSignOnType.Kerberos => CoreModels.SingleSignOnType.Kerberos,
            ConnectionSingleSignOnType.KerberosDirectQueryAndRefresh => CoreModels.SingleSignOnType.KerberosDirectQueryAndRefresh,
            ConnectionSingleSignOnType.MicrosoftEntraID => CoreModels.SingleSignOnType.MicrosoftEntraID,
            ConnectionSingleSignOnType.SecurityAssertionMarkupLanguage => CoreModels.SingleSignOnType.SecurityAssertionMarkupLanguage,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported single sign-on type '{value}'.", "credentialDetails.singleSignOnType"),
        };

    private static ConnectionSingleSignOnType ToModelSingleSignOnType(CoreModels.SingleSignOnType value)
        => value.ToString() switch
        {
            nameof(ConnectionSingleSignOnType.None) => ConnectionSingleSignOnType.None,
            nameof(ConnectionSingleSignOnType.Kerberos) => ConnectionSingleSignOnType.Kerberos,
            nameof(ConnectionSingleSignOnType.KerberosDirectQueryAndRefresh) => ConnectionSingleSignOnType.KerberosDirectQueryAndRefresh,
            nameof(ConnectionSingleSignOnType.MicrosoftEntraID) => ConnectionSingleSignOnType.MicrosoftEntraID,
            nameof(ConnectionSingleSignOnType.SecurityAssertionMarkupLanguage) => ConnectionSingleSignOnType.SecurityAssertionMarkupLanguage,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported single sign-on type '{other}' returned by the Fabric API."),
        };

    private static ConnectionCredentialType ToModelCredentialType(CoreModels.CredentialType value)
        => value.ToString() switch
        {
            nameof(ConnectionCredentialType.Anonymous) => ConnectionCredentialType.Anonymous,
            nameof(ConnectionCredentialType.Basic) => ConnectionCredentialType.Basic,
            nameof(ConnectionCredentialType.Key) => ConnectionCredentialType.Key,
            nameof(ConnectionCredentialType.KeyPair) => ConnectionCredentialType.KeyPair,
            nameof(ConnectionCredentialType.ServicePrincipal) => ConnectionCredentialType.ServicePrincipal,
            nameof(ConnectionCredentialType.SharedAccessSignature) => ConnectionCredentialType.SharedAccessSignature,
            nameof(ConnectionCredentialType.Windows) => ConnectionCredentialType.Windows,
            nameof(ConnectionCredentialType.WindowsWithoutImpersonation) => ConnectionCredentialType.WindowsWithoutImpersonation,
            nameof(ConnectionCredentialType.WorkspaceIdentity) => ConnectionCredentialType.WorkspaceIdentity,
            // OAuth2 is a valid Fabric API value, but is explicitly unsupported by this resource (matching the
            // Terraform provider, which documents that OAuth2 credentials are not supported).
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported credential type '{other}' returned by the Fabric API."),
        };

    private static Connection ToProperties(CoreModels.Connection connection)
        => new()
        {
            Id = connection.Id.ToString(),
            DisplayName = connection.DisplayName,
            ConnectivityType = connection switch
            {
                CoreModels.ShareableCloudConnection => ConnectionConnectivityType.ShareableCloud,
                CoreModels.VirtualNetworkGatewayConnection => ConnectionConnectivityType.VirtualNetworkGateway,
                _ => throw new ResourceErrorException("InvalidResponse", $"Unsupported connection type '{connection.GetType().Name}' returned by the Fabric API."),
            },
            GatewayId = connection.GatewayId?.ToString(),
            PrivacyLevel = connection.PrivacyLevel is { } privacyLevel ? ToModelPrivacyLevel(privacyLevel) : null,
            AllowConnectionUsageInGateway = (connection as CoreModels.ShareableCloudConnection)?.AllowConnectionUsageInGateway,
            AllowUsageInUserControlledCode = (connection as CoreModels.ShareableCloudConnection)?.AllowUsageInUserControlledCode,
            ConnectionDetails = new ConnectionDetailsModel
            {
                Type = connection.ConnectionDetails.Type,
                Path = connection.ConnectionDetails.Path,
            },
            CredentialDetails = new ConnectionCredentialDetails
            {
                CredentialType = connection.CredentialDetails.CredentialType is { } credentialType
                    ? ToModelCredentialType(credentialType)
                    : throw new ResourceErrorException("InvalidResponse", "The Fabric API did not return a credential type for this connection."),
                ConnectionEncryption = connection.CredentialDetails.ConnectionEncryption is { } encryption ? ToModelEncryption(encryption) : null,
                SingleSignOnType = connection.CredentialDetails.SingleSignOnType is { } sso ? ToModelSingleSignOnType(sso) : null,
                SkipTestConnection = connection.CredentialDetails.SkipTestConnection,
            },
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, Connection properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new ConnectionIdentifiers { Id = properties.Id },
        };

    private static Guid RequireId(string? id)
        => id is null
            ? throw new ResourceErrorException("MissingIdentifier", "The Connection ID is required for this operation.")
            : ParseId(id, "id");

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);
}
