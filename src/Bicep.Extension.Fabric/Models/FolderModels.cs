using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace Bicep.Extension.Fabric.Models;

public class FolderIdentifiers
{
    [TypeProperty("The containing Fabric workspace ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public string WorkspaceId { get; set; } = string.Empty;

    [TypeProperty("The Folder ID", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.ReadOnly)]
    public string? Id { get; set; }
}

[ResourceType("Folder")]
public class Folder : FolderIdentifiers
{
    [TypeProperty("The Folder display name", ObjectTypePropertyFlags.Required)]
    public required string DisplayName { get; set; }

    [TypeProperty("The parent folder ID. If not specified, the folder is created with the workspace as its parent folder.")]
    public string? ParentFolderId { get; set; }
}
