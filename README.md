# Microsoft Fabric Bicep Extension

This experimental local-deploy extension manages Microsoft Fabric resources with Bicep. It uses the official [`Microsoft.Fabric.Api`](https://www.nuget.org/packages/Microsoft.Fabric.Api) .NET SDK.

## Supported resources

The extension supports `Workspace` and the Fabric item types represented by the provider-aligned models in [`Models`](./src/Bicep.Extension.Fabric/Models), including `Lakehouse`, `Warehouse`, `Notebook`, `DataPipeline`, `SemanticModel`, and `Report`.

Fabric item resources share these properties:

- `workspaceId` and `displayName` are required.
- `description` and `folderId` are optional.
- `tags` accepts the IDs of tags to apply to the item. Tags applied outside of Bicep are left untouched when this is not set.
- `definition.parts` accepts paths and base64-encoded payloads for definition-backed item types.
- `id` and `type` are read-only outputs.

Some item types carry extra creation configuration or read-only properties — for example `Lakehouse`,
`Warehouse`, `WarehouseSnapshot`, `Eventhouse`, `KQLDatabase`, `SQLDatabase` and `DigitalTwinBuilderFlow`
each expose a `configuration` object.

`Dashboard`, `Datamart` and `MirroredWarehouse` are only exposed for listing and reading by the Fabric
API, so they can be referenced with an `existing` resource but cannot be created, updated or deleted.

Beyond items, the extension covers workspace configuration (role assignments, networking policies,
Spark settings, OneLake data access, managed private endpoints and Git connections), along with
tenant-level resources such as `Connection`, `Gateway`, `Domain`, `Tag`, `DeploymentPipeline` and
`TenantSetting`.

All Bicep properties use camelCase. The extension configuration requires a secure Microsoft Entra access token for `https://api.fabric.microsoft.com`.

## Build and test

```sh
dotnet build .
dotnet test
./scripts/publish.ps1 ./bicep-ext-fabric
```

The MSTest project runs with the Microsoft Testing Platform runner. Handler tests invoke
the public `IResourceHandler` entry points through a JSON-based test harness, keeping
serialization and camelCase behavior covered without contacting Fabric. Tests are grouped
into focused classes by handler area; run one class with:

```sh
dotnet test --filter "FullyQualifiedName~FabricRichItemHandlerTests"
```

See the [Bicep extension unit testing guide](https://github.com/Azure/bicep/blob/main/docs/experimental/local-deploy-dotnet-unittesting-guide.md)
for the recommended handler testing approach.

To use the local build, set the extension mapping in `samples/bicepconfig.json`:

```json
{
  "experimentalFeaturesEnabled": {
    "localDeploy": true
  },
  "extensions": {
    "fabric": "../bicep-ext-fabric"
  },
  "implicitExtensions": []
}
```

## Samples

- [`basic`](./samples/basic) creates a workspace and lakehouse.
- [`medallion-lakehouse`](./samples/medallion-lakehouse) creates bronze, silver, and gold lakehouses with an ingestion pipeline.
- [`data-science`](./samples/data-science) creates a feature lakehouse, shared environment, experiment, and registered model.
- [`real-time-analytics`](./samples/real-time-analytics) creates an eventhouse, KQL database, eventstream, KQL queryset, and dashboard.
- [`governance`](./samples/governance) assigns a workspace to a domain, grants domain and workspace roles, applies a tag to an item, and creates a deployment pipeline.
- [`workspace-security`](./samples/workspace-security) locks down workspace networking (Git, gateway, cloud connection, and public network policies), enables Spark workspace settings, grants OneLake data access, enables warehouse SQL auditing, and creates a Managed Private Endpoint.
- [`workspace-git`](./samples/workspace-git) provisions a workspace identity and connects the workspace to an Azure DevOps repository.

Acquire a Fabric token and deploy any sample parameter file:

```sh
export FABRIC_TOKEN=$(az account get-access-token --resource https://api.fabric.microsoft.com --query accessToken -o tsv)
bicep local-deploy ./samples/medallion-lakehouse/main.bicepparam
```

This command creates or updates real Microsoft Fabric resources. Review the sample before deploying it.

Set `BICEP_TRACING_ENABLED=true` to enable verbose local-deploy tracing.

## Releasing

Releases are cut manually so that versioning stays under explicit control — pushing to `main` does
not publish anything. To release, run the **Release** workflow from the Actions tab (or with
`gh workflow run release.yml -f version=0.2.0`) and supply the exact version to publish.

The workflow validates the version, builds and tests, publishes
`br:ghcr.io/anthony-c-martin/bicep-ext-fabric:<version>` and then pushes a `v`-prefixed git tag
(`v0.2.0`) and GitHub Release. Note that the OCI artifact is tagged with the bare version, while the
git tag carries the `v` prefix. The workflow refuses to run if the tag already exists, so published
versions are never replaced; releases must be cut from `main`.

The version supplied to the workflow is stamped into the binary via `-p:Version=`, and is what the
extension reports to Bicep. Local builds use the placeholder `0.0.1-dev` version from
[`Bicep.Extension.Fabric.csproj`](./src/Bicep.Extension.Fabric/Bicep.Extension.Fabric.csproj).

To configure this repository's GitHub branch protection and collaborators, login with the `gh` CLI and run:

```powershell
./scripts/setup.ps1
```

The script obtains a token from `GITHUB_TOKEN` or `gh auth token`, authenticates to GHCR
when Docker is available, and deploys [`scripts/repo/main.bicepparam`](./scripts/repo/main.bicepparam).