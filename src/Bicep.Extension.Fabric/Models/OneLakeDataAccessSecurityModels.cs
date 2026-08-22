using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public enum DataAccessRoleKind
{
    Policy,
}

public enum DataAccessRoleEffect
{
    Permit,
}

public enum DataAccessRoleAttributeName
{
    Path,
    Action,
}

public enum DataAccessRoleColumnAction
{
    Read,
}

public enum DataAccessRoleColumnEffect
{
    Permit,
}

public enum DataAccessRoleItemAccess
{
    Read,
    Write,
    Reshare,
    Explore,
    Execute,
    ReadAll,
}

public enum DataAccessRoleObjectType
{
    Group,
    User,
    ServicePrincipal,
    ManagedIdentity,
}

public class DataAccessRoleColumnConstraint
{
    [TypeProperty("A relative file path specifying which table the column constraint applies to, in the form /Tables/{optionalSchema}/{tableName}", ObjectTypePropertyFlags.Required)]
    public required string TablePath { get; set; }

    [TypeProperty("The case-sensitive column names the constraint applies to. Use '*' to indicate all columns", ObjectTypePropertyFlags.Required)]
    public required string[] ColumnNames { get; set; }

    [TypeProperty("The effect given to the specified column names", ObjectTypePropertyFlags.Required)]
    public required DataAccessRoleColumnEffect ColumnEffect { get; set; }

    [TypeProperty("The actions applied to the column names", ObjectTypePropertyFlags.Required)]
    public required DataAccessRoleColumnAction[] ColumnAction { get; set; }
}

public class DataAccessRoleRowConstraint
{
    [TypeProperty("A relative file path specifying which table the row constraint applies to, in the form /Tables/{optionalSchema}/{tableName}", ObjectTypePropertyFlags.Required)]
    public required string TablePath { get; set; }

    [TypeProperty("A T-SQL expression used to evaluate which rows the role members can see", ObjectTypePropertyFlags.Required)]
    public required string Value { get; set; }
}

public class DataAccessRoleConstraints
{
    [TypeProperty("Column-level constraints applied to one or more tables in the data access role")]
    public DataAccessRoleColumnConstraint[]? Columns { get; set; }

    [TypeProperty("Row-level constraints applied to one or more tables in the data access role")]
    public DataAccessRoleRowConstraint[]? Rows { get; set; }
}

public class DataAccessRolePermissionScope
{
    [TypeProperty("The name of the attribute being evaluated for access permissions", ObjectTypePropertyFlags.Required)]
    public required DataAccessRoleAttributeName AttributeName { get; set; }

    [TypeProperty("The allowed values for this attribute", ObjectTypePropertyFlags.Required)]
    public required string[] AttributeValueIncludedIn { get; set; }
}

public class DataAccessRoleDecisionRule
{
    [TypeProperty("The effect that this rule has on access to the data resource", ObjectTypePropertyFlags.Required)]
    public required DataAccessRoleEffect Effect { get; set; }

    [TypeProperty("The permissions defined by attribute name and values", ObjectTypePropertyFlags.Required)]
    public required DataAccessRolePermissionScope[] Permission { get; set; }

    [TypeProperty("Row or column level constraints applied to tables as part of this rule. If omitted, no constraints apply")]
    public DataAccessRoleConstraints? Constraints { get; set; }
}

public class DataAccessRoleFabricItemMember
{
    [TypeProperty("The permissions granted for the item", ObjectTypePropertyFlags.Required)]
    public required DataAccessRoleItemAccess[] ItemAccess { get; set; }

    [TypeProperty("The path to the Fabric item having the specified item access, as two GUIDs separated by a slash", ObjectTypePropertyFlags.Required)]
    public required string SourcePath { get; set; }
}

public class DataAccessRoleMicrosoftEntraMember
{
    [TypeProperty("The Microsoft Entra object ID", ObjectTypePropertyFlags.Required)]
    public required string ObjectId { get; set; }

    [TypeProperty("The type of Microsoft Entra object", ObjectTypePropertyFlags.Required)]
    public required DataAccessRoleObjectType ObjectType { get; set; }

    [TypeProperty("The Microsoft Entra tenant ID", ObjectTypePropertyFlags.Required)]
    public required string TenantId { get; set; }
}

public class DataAccessRoleMembers
{
    [TypeProperty("Fabric-scoped members with path-based access")]
    public DataAccessRoleFabricItemMember[]? FabricItemMembers { get; set; }

    [TypeProperty("Microsoft Entra ID members")]
    public DataAccessRoleMicrosoftEntraMember[]? MicrosoftEntraMembers { get; set; }
}

public class OneLakeDataAccessSecurityIdentifiers
{
    [TypeProperty("The containing Fabric workspace ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string WorkspaceId { get; set; } = string.Empty;

    [TypeProperty("The ID of the Fabric item the data access role applies to", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string ItemId { get; set; } = string.Empty;

    [TypeProperty("The name of the data access role", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string RoleName { get; set; } = string.Empty;
}

[ResourceType("OneLakeDataAccessSecurity")]
public class OneLakeDataAccessSecurity : OneLakeDataAccessSecurityIdentifiers
{
    [TypeProperty("The kind of the data access role")]
    public DataAccessRoleKind? Kind { get; set; }

    [TypeProperty("The permissions that make up the data access role", ObjectTypePropertyFlags.Required)]
    public required DataAccessRoleDecisionRule[] DecisionRules { get; set; }

    [TypeProperty("The members of the role", ObjectTypePropertyFlags.Required)]
    public required DataAccessRoleMembers Members { get; set; }
}
