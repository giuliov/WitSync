# Visual Studio / Team Foundation Server internal version
$version = "12" # i.e. 2013

# where is witadmin located?
if ($env:ProgramW6432) {
    # 64 bit
    $vsPath = Get-ItemProperty -Path HKLM:\SOFTWARE\Wow6432Node\Microsoft\VisualStudio\$version.0 -Name InstallDir
} else {
    # 32 bit
    $vsPath = Get-ItemProperty -Path HKLM:\SOFTWARE\Microsoft\VisualStudio\$version.0 -Name InstallDir
}
$witAdmin = Join-Path $vsPath.InstallDir -ChildPath 'witAdmin.exe'

if (-not (Test-Path $witAdmin)) {
    Write-Error "Team Explorer is not installed"
    exit
}

# where is TfsConfig ?
$tfsPath = Get-ItemProperty -Path HKLM:\SOFTWARE\Microsoft\TeamFoundationServer\$version.0 -Name InstallPath
$TfsConfig = Join-Path $tfsPath.InstallPath -ChildPath 'Tools\TfsConfig.exe'
$TfsServiceControl = Join-Path $tfsPath.InstallPath -ChildPath 'Tools\TfsServiceControl.exe'

$sqlDataPath = 'C:\Program Files\Microsoft SQL Server\MSSQL11.SQLEXPRESS\MSSQL\DATA'
$dataSavePath = "$env:USERPROFILE\Source\Repos\SharedTools-GlobalIT\WitSync\WitSyncEngine.Tests\Database"