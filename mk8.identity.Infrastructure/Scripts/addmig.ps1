param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Target,

    [Parameter(Mandatory = $true, Position = 1)]
    [string]$Name
)

$ErrorActionPreference = 'Stop'

# Aliases: id/identity, ap/app/application
switch -Regex ($Target.ToLower()) {
    '^(id|identity)$'           { Add-Migration $Name -Context IdentityContext -OutputDir 'Migrations/Identity'; break }
    '^(ap|app|application)$'    { Add-Migration $Name -Context ApplicationContext -OutputDir 'Migrations/Application'; break }
    default                     { Add-Migration $Name -Context $Target; break }
}
