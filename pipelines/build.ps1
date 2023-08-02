dotnet restore
dotnet build -c Release --no-restore

$scriptpath = $MyInvocation.MyCommand.Path
$dir = Split-Path $scriptpath

Write-Output $dir

$branch = $env:BUILD_SOURCEBRANCH

Write-Output "Source branch: $branch"
$versionSuffix = $null
$outputDirectory = $args[0]

$expr = "dotnet pack -c 'Release' -o '$outputDirectory' "

if ($branch -ne "refs/heads/main")
{
    $versionSuffix = Get-Date -Format yyyyMMddHHmm
    $versionSuffix = "pre.$versionSuffix"
    $expr = $expr + "--version-suffix '$versionSuffix' "

    Write-Output "Packaging version: $versionSuffix"
}

$projectPaths = Get-ChildItem -Path $dir\.. -Recurse | where { $_.extension -eq ".csproj" }
foreach ($projectPath in $projectPaths)
{
    $fullName = $projectPath.FullName
    Write-Output "Invoke-Expression: $expr $fullName"
    Invoke-Expression ($expr + " " + $fullName)
}

Write-Output "Created packages in ${outputDirectory}: "
ls -lah $outputDirectory
