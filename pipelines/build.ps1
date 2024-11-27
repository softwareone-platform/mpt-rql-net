param ( 
    [Parameter(Mandatory=$true)]
    [string]$Version,
    [string]$outputDirectory 
) 

dotnet restore
dotnet build -c Release --no-restore -p:Version=$Version

$scriptpath = $MyInvocation.MyCommand.Path
$dir = Split-Path $scriptpath

Write-Output $dir

$branch = $env:BUILD_SOURCEBRANCH

Write-Output "Source branch: $branch"
$versionSuffix = $null

$expr = "dotnet pack -c 'Release' -o '$outputDirectory' "

$projectPaths = Get-ChildItem -Path $dir\.. -Recurse | where { $_.extension -eq ".csproj" }
foreach ($projectPath in $projectPaths)
{
    $fullName = $projectPath.FullName
    Write-Output "Invoke-Expression: $expr $fullName"
    Invoke-Expression ($expr + " " + $fullName)
}

Write-Output "Created packages in ${outputDirectory}: "
ls -lah $outputDirectory
