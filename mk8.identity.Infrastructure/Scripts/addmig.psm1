function Resolve-ContextName {
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Context
    )

    # Aliases: id/identity, ap/app/application
    switch -Regex ($Context.ToLower()) {
        '^(id|identity)$'           { return 'IdentityContext' }
        '^(ap|app|application)$'    { return 'ApplicationContext' }
        default                     { return $Context }
    }
}

function Add-Mig {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Target,

        [Parameter(Mandatory = $true, Position = 1)]
        [string]$Name
    )

    $contextName = Resolve-ContextName $Target

    switch ($contextName) {
        'IdentityContext'    { Add-Migration $Name -Context $contextName -OutputDir 'Migrations/Identity'; break }
        'ApplicationContext' { Add-Migration $Name -Context $contextName -OutputDir 'Migrations/Application'; break }
        default              { Add-Migration $Name -Context $contextName; break }
    }
}

Set-Alias addmig Add-Mig

function Up-Db {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Target
    )

    $contextName = Resolve-ContextName $Target
    Update-Database -Context $contextName
}

Set-Alias updb Up-Db
