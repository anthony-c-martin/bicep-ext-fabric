using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum ConnectionConnectivityType
{
    ShareableCloud,
    VirtualNetworkGateway,
}

public enum ConnectionPrivacyLevel
{
    None,
    Organizational,
    Private,
    Public,
}

public enum ConnectionCredentialType
{
    Anonymous,
    Basic,
    Key,
    KeyPair,
    ServicePrincipal,
    SharedAccessSignature,
    Windows,
    WindowsWithoutImpersonation,
    WorkspaceIdentity,
}

public enum ConnectionEncryptionType
{
    Any,
    Encrypted,
    NotEncrypted,
}

public enum ConnectionSingleSignOnType
{
    None,
    Kerberos,
    KerberosDirectQueryAndRefresh,
    MicrosoftEntraID,
    SecurityAssertionMarkupLanguage,
}

// The Fabric API supports typed parameters (text/number/boolean/date/etc.), but the Terraform
// provider only ever sends string values for connection parameters, so this resource does the
// same (always constructing a text parameter).
public enum ConnectionParameterDataType
{
    Text,
    Number,
    Boolean,
    Date,
    DateTime,
    DateTimeZone,
    Duration,
    Time,
}

public class ConnectionDetailsParameter
{
    [TypeProperty("The name of the parameter", ObjectTypePropertyFlags.Required)]
    public required string Name { get; set; }

    [TypeProperty("The value of the parameter, expressed as a string. Numbers use invariant formatting, booleans use 'true'/'false', and date, time and duration values use ISO 8601.", ObjectTypePropertyFlags.Required)]
    public required string Value { get; set; }

    [TypeProperty("The data type of the parameter. Defaults to Text.")]
    public ConnectionParameterDataType? DataType { get; set; }
}

public class ConnectionDetailsModel
{
    [TypeProperty("The type of the connection", ObjectTypePropertyFlags.Required)]
    public required string Type { get; set; }

    // Not returned by GetConnection (only type/path are echoed back on read), so this is left
    // unset (empty) on the Get path rather than marked as a C#-`required` member.
    [TypeProperty("The creation method used to create the connection", ObjectTypePropertyFlags.Required)]
    public string CreationMethod { get; set; } = string.Empty;

    [TypeProperty("The connection parameters")]
    public ConnectionDetailsParameter[]? Parameters { get; set; }

    [TypeProperty("The path of the connection", ObjectTypePropertyFlags.ReadOnly)]
    public string? Path { get; set; }
}

public class ConnectionKeyVaultSecretReference
{
    [TypeProperty("The connection ID of the Key Vault connection", ObjectTypePropertyFlags.Required)]
    public required string ConnectionId { get; set; }

    [TypeProperty("The name of the secret in Key Vault", ObjectTypePropertyFlags.Required)]
    public required string SecretName { get; set; }

    [TypeProperty("The version of the secret in Key Vault")]
    public string? Version { get; set; }
}

public class ConnectionBasicCredentials
{
    [TypeProperty("The username", ObjectTypePropertyFlags.Required)]
    public required string Username { get; set; }

    [TypeProperty("The password", isSecure: true)]
    public string? Password { get; set; }

    [TypeProperty("The Key Vault reference for the password secret")]
    public ConnectionKeyVaultSecretReference? PasswordReference { get; set; }
}

public class ConnectionKeyCredentials
{
    [TypeProperty("The key", isSecure: true)]
    public string? Key { get; set; }

    [TypeProperty("The Key Vault reference for the key secret")]
    public ConnectionKeyVaultSecretReference? KeyReference { get; set; }
}

public class ConnectionKeyPairCredentials
{
    [TypeProperty("The identifier for the key", ObjectTypePropertyFlags.Required)]
    public required string Identifier { get; set; }

    [TypeProperty("The private key based on the PKCS #8 standard", ObjectTypePropertyFlags.Required, isSecure: true)]
    public required string PrivateKey { get; set; }

    [TypeProperty("The passphrase for the private key if it is encrypted", isSecure: true)]
    public string? Passphrase { get; set; }
}

public class ConnectionServicePrincipalCredentials
{
    [TypeProperty("The tenant ID", ObjectTypePropertyFlags.Required)]
    public required string TenantId { get; set; }

    [TypeProperty("The client ID", ObjectTypePropertyFlags.Required)]
    public required string ClientId { get; set; }

    [TypeProperty("The client secret", isSecure: true)]
    public string? ClientSecret { get; set; }

    [TypeProperty("The Key Vault reference for the client secret")]
    public ConnectionKeyVaultSecretReference? ClientSecretReference { get; set; }
}

public class ConnectionSharedAccessSignatureCredentials
{
    [TypeProperty("The token", isSecure: true)]
    public string? Token { get; set; }

    [TypeProperty("The Key Vault reference for the SAS token secret")]
    public ConnectionKeyVaultSecretReference? TokenReference { get; set; }
}

public class ConnectionWindowsCredentials
{
    [TypeProperty("The username", ObjectTypePropertyFlags.Required)]
    public required string Username { get; set; }

    [TypeProperty("The password", ObjectTypePropertyFlags.Required, isSecure: true)]
    public required string Password { get; set; }
}

public class ConnectionCredentialDetails
{
    [TypeProperty("The credential type", ObjectTypePropertyFlags.Required)]
    public required ConnectionCredentialType CredentialType { get; set; }

    [TypeProperty("The connection encryption type. Defaults to NotEncrypted")]
    public ConnectionEncryptionType? ConnectionEncryption { get; set; }

    [TypeProperty("The single sign-on type. Defaults to None")]
    public ConnectionSingleSignOnType? SingleSignOnType { get; set; }

    [TypeProperty("Whether the connection should skip the test connection during creation and update. Defaults to false")]
    public bool? SkipTestConnection { get; set; }

    [TypeProperty("The basic credentials. Required when credentialType is Basic")]
    public ConnectionBasicCredentials? Basic { get; set; }

    [TypeProperty("The key credentials. Required when credentialType is Key")]
    public ConnectionKeyCredentials? Key { get; set; }

    [TypeProperty("The key pair credentials. Required when credentialType is KeyPair")]
    public ConnectionKeyPairCredentials? KeyPair { get; set; }

    [TypeProperty("The service principal credentials. Required when credentialType is ServicePrincipal")]
    public ConnectionServicePrincipalCredentials? ServicePrincipal { get; set; }

    [TypeProperty("The shared access signature credentials. Required when credentialType is SharedAccessSignature")]
    public ConnectionSharedAccessSignatureCredentials? SharedAccessSignature { get; set; }

    [TypeProperty("The Windows credentials. Required when credentialType is Windows")]
    public ConnectionWindowsCredentials? Windows { get; set; }
}

public class ConnectionIdentifiers
{
    [TypeProperty("The Connection ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }
}

[ResourceType("Connection")]
public class Connection : ConnectionIdentifiers
{
    [TypeProperty("The Connection display name", ObjectTypePropertyFlags.Required)]
    public required string DisplayName { get; set; }

    [TypeProperty("The Connection connectivity type", ObjectTypePropertyFlags.Required)]
    public required ConnectionConnectivityType ConnectivityType { get; set; }

    [TypeProperty("The Connection gateway object ID. Required when connectivityType is VirtualNetworkGateway")]
    public string? GatewayId { get; set; }

    [TypeProperty("The Connection privacy level. Defaults to Organizational")]
    public ConnectionPrivacyLevel? PrivacyLevel { get; set; }

    [TypeProperty("Allow this connection to be utilized with on-premises or VNet data gateways. Not applicable when connectivityType is VirtualNetworkGateway")]
    public bool? AllowConnectionUsageInGateway { get; set; }

    [TypeProperty("Allow this connection to be used with items that allow user-controlled code such as Notebook. Not applicable when connectivityType is VirtualNetworkGateway")]
    public bool? AllowUsageInUserControlledCode { get; set; }

    [TypeProperty("The Connection connection details", ObjectTypePropertyFlags.Required)]
    public required ConnectionDetailsModel ConnectionDetails { get; set; }

    [TypeProperty("The Connection credential details", ObjectTypePropertyFlags.Required)]
    public required ConnectionCredentialDetails CredentialDetails { get; set; }
}
