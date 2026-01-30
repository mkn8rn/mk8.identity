param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Target
)

$ErrorActionPreference = 'Stop'

# Aliases: id/identity, ap/app/application
switch -Regex ($Target.ToLower()) {
    '^(id|identity)$'           { Update-Database -Context IdentityContext; break }
    '^(ap|app|application)$'    { Update-Database -Context ApplicationContext; break }
    default                     { Update-Database -Context $Target; break }
}
