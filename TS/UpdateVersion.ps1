# Enable -Verbose option
[CmdletBinding()]

# Regular expression pattern to find the version in the build number 
#$VersionRegex = "\d+\.\d+\.\d+\.\d+"
$VersionRegex = "[0-9]+(\.([0-9]+|\*)){1,3}"

# Make sure there is a build number
if (-not $Env:BUILD_BUILDNUMBER)
{
    Write-Error ("BUILD_BUILDNUMBER environment variable is missing.")
    exit 1
}
Write-Host "BUILD_BUILDNUMBER: $Env:BUILD_BUILDNUMBER"


$files = gci . -recurse -include AssemblyInfo.cs
if($files)
{
    Write-Host "Will apply $Env:BUILD_BUILDNUMBER to $($files.count) AssemblyInfo files."
    
    $assemblyVersionPattern = 'AssemblyVersion\("' + $VersionRegex + '"\)'
    $assemblyVersion = 'AssemblyVersion("' + $Env:BUILD_BUILDNUMBER + '")';
    
    $fileVersionPattern = 'AssemblyFileVersion\("' + $VersionRegex + '"\)'
    $fileVersion = 'AssemblyFileVersion("' + $Env:BUILD_BUILDNUMBER + '")';

    foreach ($file in $files) {
       (Get-Content $file) | ForEach-Object {
            % {$_ -replace $assemblyVersionPattern, $assemblyVersion } |
            % {$_ -replace $fileVersionPattern, $fileVersion }
       } | Set-Content $file

       Write-Host "$file - version applied"
    }
}
else
{
    Write-Error "Found no AssemblyInfo files."
	exit 1
}

$files = gci . -recurse -include Package.nuspec
if($files)
{
    Write-Host "Will apply $Env:BUILD_BUILDNUMBER to $($files.count) Nuspec file."
    
    $nuspecVersionPattern = '<version>' + $VersionRegex + '</version>'
    $nuspecVersion = '<version>' + $Env:BUILD_BUILDNUMBER + '</version>';

    foreach ($file in $files) {
       (Get-Content $file) | ForEach-Object {
            % {$_ -replace $nuspecVersionPattern, $nuspecVersion }
       } | Set-Content $file

       Write-Host "$file - version applied"
    }
}
else
{
    Write-Error "Found no Package.nuspec file."
	exit 1
}

$files = gci . -recurse -include *.NetStandard.csproj
if($files)
{
    Write-Host "Will apply $Env:BUILD_BUILDNUMBER to $($files.count) NetStandard.csproj file."
    
    $versionPattern = '<Version>' + $VersionRegex + '</Version>'
    $version = '<Version>' + $Env:BUILD_BUILDNUMBER + '</Version>';

	$assemblyVersionPattern = '<AssemblyVersion>' + $VersionRegex + '</AssemblyVersion>'
    $assemblyVersion = '<AssemblyVersion>' + $Env:BUILD_BUILDNUMBER + '</AssemblyVersion>';

	$fileVersionPattern = '<FileVersion>' + $VersionRegex + '</FileVersion>'
    $fileVersion = '<FileVersion>' + $Env:BUILD_BUILDNUMBER + '</FileVersion>';

    foreach ($file in $files) {
       (Get-Content $file) | ForEach-Object {
            % {$_ -replace $versionPattern, $version } |
			% {$_ -replace $assemblyVersionPattern, $assemblyVersion } |
			% {$_ -replace $fileVersionPattern, $fileVersion }
       } | Set-Content $file

       Write-Host "$file - version applied"
    }
}
else
{
    Write-Error "Found no NetStandard.csproj file."
	exit 1
}