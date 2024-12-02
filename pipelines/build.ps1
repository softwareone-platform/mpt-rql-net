param ( 
    [Parameter(Mandatory=$true)]
    [string]$Version,
    [string]$outputDirectory 
) 

dotnet restore
dotnet build -c Release --no-restore -p:Version=$Version

$scriptpath = $MyInvocation.MyCommand.Path
$dir = Split-Path $scriptpath

write-output "Version: $Version"
write-output "Directory: $dir"

$branch = $env:BUILD_SOURCEBRANCH

write-output "Source branch: $branch"

$expr = "dotnet pack -c 'Release' -o '$outputDirectory'  -p:Version=$Version"

$projectPaths = Get-ChildItem -Path $dir\.. -Recurse | where { $_.extension -eq ".csproj" }
foreach ($projectPath in $projectPaths)
{
    $fullName = $projectPath.FullName
    Write-Output "Invoke-Expression: $expr $fullName"
    Invoke-Expression ($expr + " " + $fullName)
}

Write-Output "Created packages in ${outputDirectory}: "
ls -lah $outputDirectory
