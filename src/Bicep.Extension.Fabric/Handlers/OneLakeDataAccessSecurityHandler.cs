using CoreModels = Microsoft.Fabric.Api.Core.Models;

namespace Bicep.Extension.Fabric.Handlers;

public sealed class OneLakeDataAccessSecurityHandler : FabricResourceHandlerBase<OneLakeDataAccessSecurity, OneLakeDataAccessSecurityIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
        => Task.FromResult(GetResponse(request));

    protected override Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Properties.WorkspaceId, "workspaceId");
            var itemId = ParseId(request.Properties.ItemId, "itemId");

            await client.Core.OneLakeDataAccessSecurity.CreateOrUpdateSingleDataAccessRoleAsync(
                workspaceId,
                itemId,
                ToSdkRole(request.Properties),
                dataAccessRoleConflictPolicy: CoreModels.DataAccessRoleConflictPolicy.Overwrite,
                ifMatch: null,
                ifNoneMatch: null,
                cancellationToken);

            var result = (await client.Core.OneLakeDataAccessSecurity.GetDataAccessRoleAsync(
                workspaceId,
                itemId,
                request.Properties.RoleName,
                ifMatch: null,
                ifNoneMatch: null,
                cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId, itemId));
        });

    protected override Task<ResourceResponse> Get(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            var workspaceId = ParseId(request.Identifiers.WorkspaceId, "workspaceId");
            var itemId = ParseId(request.Identifiers.ItemId, "itemId");

            var result = (await client.Core.OneLakeDataAccessSecurity.GetDataAccessRoleAsync(
                workspaceId,
                itemId,
                request.Identifiers.RoleName,
                ifMatch: null,
                ifNoneMatch: null,
                cancellationToken)).Value;

            return BuildResponse(request.Type, request.ApiVersion, ToProperties(result, workspaceId, itemId));
        });

    protected override Task<ResourceResponse> Delete(ReferenceRequest request, CancellationToken cancellationToken)
        => HandleRequest(request, async client =>
        {
            await client.Core.OneLakeDataAccessSecurity.DeleteDataAccessRoleAsync(
                ParseId(request.Identifiers.WorkspaceId, "workspaceId"),
                ParseId(request.Identifiers.ItemId, "itemId"),
                request.Identifiers.RoleName,
                cancellationToken);

            return GetResponse(request, properties: null);
        });

    protected override OneLakeDataAccessSecurityIdentifiers GetIdentifiers(OneLakeDataAccessSecurity properties)
        => new() { WorkspaceId = properties.WorkspaceId, ItemId = properties.ItemId, RoleName = properties.RoleName };

    private static CoreModels.DataAccessRoleBase ToSdkRole(OneLakeDataAccessSecurity properties)
    {
        var decisionRules = properties.DecisionRules.Select(ToSdkDecisionRule).ToList();
        var role = new CoreModels.DataAccessRoleBase(properties.RoleName, decisionRules)
        {
            Members = ToSdkMembers(properties.Members),
        };

        if (properties.Kind is { } kind)
        {
            role.Kind = ToSdkKind(kind);
        }

        return role;
    }

    private static CoreModels.DecisionRule ToSdkDecisionRule(DataAccessRoleDecisionRule rule)
    {
        var permissions = rule.Permission.Select(ToSdkPermissionScope).ToList();
        var sdkRule = new CoreModels.DecisionRule(permissions) { Effect = ToSdkEffect(rule.Effect) };

        if (rule.Constraints is { } constraints)
        {
            sdkRule.Constraints = ToSdkConstraints(constraints);
        }

        return sdkRule;
    }

    private static CoreModels.PermissionScope ToSdkPermissionScope(DataAccessRolePermissionScope scope)
        => new(ToSdkAttributeName(scope.AttributeName), scope.AttributeValueIncludedIn);

    private static CoreModels.DecisionRuleConstraints ToSdkConstraints(DataAccessRoleConstraints constraints)
    {
        var sdk = new CoreModels.DecisionRuleConstraints();

        if (constraints.Columns is { } columns)
        {
            foreach (var column in columns)
            {
                sdk.Columns.Add(new CoreModels.ColumnConstraint(
                    column.TablePath,
                    column.ColumnNames,
                    ToSdkColumnEffect(column.ColumnEffect),
                    column.ColumnAction.Select(ToSdkColumnAction)));
            }
        }

        if (constraints.Rows is { } rows)
        {
            foreach (var row in rows)
            {
                sdk.Rows.Add(new CoreModels.RowConstraint(row.TablePath, row.Value));
            }
        }

        return sdk;
    }

    private static CoreModels.Members ToSdkMembers(DataAccessRoleMembers members)
    {
        var sdk = new CoreModels.Members();

        if (members.FabricItemMembers is { } fabricItemMembers)
        {
            foreach (var member in fabricItemMembers)
            {
                sdk.FabricItemMembers.Add(new CoreModels.FabricItemMember(member.ItemAccess.Select(ToSdkItemAccess), member.SourcePath));
            }
        }

        if (members.MicrosoftEntraMembers is { } entraMembers)
        {
            foreach (var member in entraMembers)
            {
                sdk.MicrosoftEntraMembers.Add(new CoreModels.MicrosoftEntraMember(
                    ParseId(member.TenantId, "members.microsoftEntraMembers.tenantId"),
                    ParseId(member.ObjectId, "members.microsoftEntraMembers.objectId"))
                {
                    ObjectType = ToSdkObjectType(member.ObjectType),
                });
            }
        }

        return sdk;
    }

    private static OneLakeDataAccessSecurity ToProperties(CoreModels.DataAccessRoleBase role, Guid workspaceId, Guid itemId)
        => new()
        {
            WorkspaceId = workspaceId.ToString(),
            ItemId = itemId.ToString(),
            RoleName = role.Name,
            Kind = role.Kind is { } kind ? ToModelKind(kind) : null,
            DecisionRules = [.. role.DecisionRules.Select(ToModelDecisionRule)],
            Members = ToModelMembers(role.Members ?? new CoreModels.Members()),
        };

    private static DataAccessRoleDecisionRule ToModelDecisionRule(CoreModels.DecisionRule rule)
        => new()
        {
            Effect = ToModelEffect(rule.Effect),
            Permission = [.. rule.Permission.Select(ToModelPermissionScope)],
            Constraints = rule.Constraints is { } constraints ? ToModelConstraints(constraints) : null,
        };

    private static DataAccessRolePermissionScope ToModelPermissionScope(CoreModels.PermissionScope scope)
        => new()
        {
            AttributeName = ToModelAttributeName(scope.AttributeName),
            AttributeValueIncludedIn = [.. scope.AttributeValueIncludedIn],
        };

    private static DataAccessRoleConstraints ToModelConstraints(CoreModels.DecisionRuleConstraints constraints)
        => new()
        {
            Columns = constraints.Columns is { Count: > 0 }
                ? [.. constraints.Columns.Select(column => new DataAccessRoleColumnConstraint
                {
                    TablePath = column.TablePath,
                    ColumnNames = [.. column.ColumnNames],
                    ColumnEffect = ToModelColumnEffect(column.ColumnEffect),
                    ColumnAction = [.. column.ColumnAction.Select(ToModelColumnAction)],
                })]
                : null,
            Rows = constraints.Rows is { Count: > 0 }
                ? [.. constraints.Rows.Select(row => new DataAccessRoleRowConstraint { TablePath = row.TablePath, Value = row.Value })]
                : null,
        };

    private static DataAccessRoleMembers ToModelMembers(CoreModels.Members members)
        => new()
        {
            FabricItemMembers = members.FabricItemMembers is { Count: > 0 }
                ? [.. members.FabricItemMembers.Select(member => new DataAccessRoleFabricItemMember
                {
                    ItemAccess = [.. member.ItemAccess.Select(ToModelItemAccess)],
                    SourcePath = member.SourcePath,
                })]
                : null,
            MicrosoftEntraMembers = members.MicrosoftEntraMembers is { Count: > 0 }
                ? [.. members.MicrosoftEntraMembers.Select(member => new DataAccessRoleMicrosoftEntraMember
                {
                    ObjectId = member.ObjectId.ToString(),
                    ObjectType = member.ObjectType is { } objectType ? ToModelObjectType(objectType) : throw new ResourceErrorException("InvalidResponse", "The Fabric API returned a Microsoft Entra member without an object type."),
                    TenantId = member.TenantId.ToString(),
                })]
                : null,
        };

    private static CoreModels.DataAccessRoleKind ToSdkKind(DataAccessRoleKind kind)
        => kind switch
        {
            DataAccessRoleKind.Policy => CoreModels.DataAccessRoleKind.Policy,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported data access role kind '{kind}'.", "kind"),
        };

    private static DataAccessRoleKind ToModelKind(CoreModels.DataAccessRoleKind kind)
        => kind.ToString() switch
        {
            nameof(DataAccessRoleKind.Policy) => DataAccessRoleKind.Policy,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported data access role kind '{other}' returned by the Fabric API."),
        };

    private static CoreModels.Effect ToSdkEffect(DataAccessRoleEffect effect)
        => effect switch
        {
            DataAccessRoleEffect.Permit => CoreModels.Effect.Permit,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported effect '{effect}'.", "decisionRules.effect"),
        };

    private static DataAccessRoleEffect ToModelEffect(CoreModels.Effect? effect)
        => effect?.ToString() switch
        {
            nameof(DataAccessRoleEffect.Permit) => DataAccessRoleEffect.Permit,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported effect '{other}' returned by the Fabric API."),
        };

    private static CoreModels.AttributeName ToSdkAttributeName(DataAccessRoleAttributeName value)
        => value switch
        {
            DataAccessRoleAttributeName.Path => CoreModels.AttributeName.Path,
            DataAccessRoleAttributeName.Action => CoreModels.AttributeName.Action,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported attribute name '{value}'.", "decisionRules.permission.attributeName"),
        };

    private static DataAccessRoleAttributeName ToModelAttributeName(CoreModels.AttributeName? value)
        => value?.ToString() switch
        {
            nameof(DataAccessRoleAttributeName.Path) => DataAccessRoleAttributeName.Path,
            nameof(DataAccessRoleAttributeName.Action) => DataAccessRoleAttributeName.Action,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported attribute name '{other}' returned by the Fabric API."),
        };

    private static CoreModels.ColumnAction ToSdkColumnAction(DataAccessRoleColumnAction value)
        => value switch
        {
            DataAccessRoleColumnAction.Read => CoreModels.ColumnAction.Read,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported column action '{value}'.", "decisionRules.constraints.columns.columnAction"),
        };

    private static DataAccessRoleColumnAction ToModelColumnAction(CoreModels.ColumnAction value)
        => value.ToString() switch
        {
            nameof(DataAccessRoleColumnAction.Read) => DataAccessRoleColumnAction.Read,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported column action '{other}' returned by the Fabric API."),
        };

    private static CoreModels.ColumnEffect ToSdkColumnEffect(DataAccessRoleColumnEffect value)
        => value switch
        {
            DataAccessRoleColumnEffect.Permit => CoreModels.ColumnEffect.Permit,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported column effect '{value}'.", "decisionRules.constraints.columns.columnEffect"),
        };

    private static DataAccessRoleColumnEffect ToModelColumnEffect(CoreModels.ColumnEffect? value)
        => value?.ToString() switch
        {
            nameof(DataAccessRoleColumnEffect.Permit) => DataAccessRoleColumnEffect.Permit,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported column effect '{other}' returned by the Fabric API."),
        };

    private static CoreModels.ItemAccess ToSdkItemAccess(DataAccessRoleItemAccess value)
        => value switch
        {
            DataAccessRoleItemAccess.Read => CoreModels.ItemAccess.Read,
            DataAccessRoleItemAccess.Write => CoreModels.ItemAccess.Write,
            DataAccessRoleItemAccess.Reshare => CoreModels.ItemAccess.Reshare,
            DataAccessRoleItemAccess.Explore => CoreModels.ItemAccess.Explore,
            DataAccessRoleItemAccess.Execute => CoreModels.ItemAccess.Execute,
            DataAccessRoleItemAccess.ReadAll => CoreModels.ItemAccess.ReadAll,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported item access '{value}'.", "members.fabricItemMembers.itemAccess"),
        };

    private static DataAccessRoleItemAccess ToModelItemAccess(CoreModels.ItemAccess value)
        => value.ToString() switch
        {
            nameof(DataAccessRoleItemAccess.Read) => DataAccessRoleItemAccess.Read,
            nameof(DataAccessRoleItemAccess.Write) => DataAccessRoleItemAccess.Write,
            nameof(DataAccessRoleItemAccess.Reshare) => DataAccessRoleItemAccess.Reshare,
            nameof(DataAccessRoleItemAccess.Explore) => DataAccessRoleItemAccess.Explore,
            nameof(DataAccessRoleItemAccess.Execute) => DataAccessRoleItemAccess.Execute,
            nameof(DataAccessRoleItemAccess.ReadAll) => DataAccessRoleItemAccess.ReadAll,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported item access '{other}' returned by the Fabric API."),
        };

    private static CoreModels.ObjectType ToSdkObjectType(DataAccessRoleObjectType value)
        => value switch
        {
            DataAccessRoleObjectType.Group => CoreModels.ObjectType.Group,
            DataAccessRoleObjectType.User => CoreModels.ObjectType.User,
            DataAccessRoleObjectType.ServicePrincipal => CoreModels.ObjectType.ServicePrincipal,
            DataAccessRoleObjectType.ManagedIdentity => CoreModels.ObjectType.ManagedIdentity,
            _ => throw new ResourceErrorException("InvalidProperty", $"Unsupported object type '{value}'.", "members.microsoftEntraMembers.objectType"),
        };

    private static DataAccessRoleObjectType ToModelObjectType(CoreModels.ObjectType value)
        => value.ToString() switch
        {
            nameof(DataAccessRoleObjectType.Group) => DataAccessRoleObjectType.Group,
            nameof(DataAccessRoleObjectType.User) => DataAccessRoleObjectType.User,
            nameof(DataAccessRoleObjectType.ServicePrincipal) => DataAccessRoleObjectType.ServicePrincipal,
            nameof(DataAccessRoleObjectType.ManagedIdentity) => DataAccessRoleObjectType.ManagedIdentity,
            var other => throw new ResourceErrorException("InvalidResponse", $"Unsupported object type '{other}' returned by the Fabric API."),
        };

    private static ResourceResponse BuildResponse(string type, string? apiVersion, OneLakeDataAccessSecurity properties)
        => new()
        {
            Type = type,
            ApiVersion = apiVersion,
            Properties = properties,
            Identifiers = new OneLakeDataAccessSecurityIdentifiers
            {
                WorkspaceId = properties.WorkspaceId,
                ItemId = properties.ItemId,
                RoleName = properties.RoleName,
            },
        };

    private static Guid ParseId(string value, string target)
        => Guid.TryParse(value, out var result)
            ? result
            : throw new ResourceErrorException("InvalidIdentifier", $"'{value}' is not a valid GUID.", target);
}
