param(
    [string]$InputDirectory=(Join-Path $PSScriptRoot '../Builds/Linux'),
    [string]$OutputDirectory=(Join-Path $PSScriptRoot '../Distribution')
)
$ErrorActionPreference='Stop'
$linuxInput=(Resolve-Path -LiteralPath $InputDirectory).Path
if(-not (Test-Path -LiteralPath (Join-Path $linuxInput 'TrajetoInclusivo.x86_64'))){throw 'Gere o build Linux antes de empacotar.'}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$linuxArchive=Join-Path (Resolve-Path -LiteralPath $OutputDirectory).Path 'TrajetoInclusivo-Linux-x86_64.tar.gz'
$archiveStream=[IO.File]::Create($linuxArchive)
$gzipStream=[IO.Compression.GZipStream]::new($archiveStream,[IO.Compression.CompressionLevel]::Optimal,$false)
$tarWriter=[System.Formats.Tar.TarWriter]::new($gzipStream,$true)
try {
    foreach($file in Get-ChildItem -LiteralPath $linuxInput -File -Recurse | Sort-Object FullName) {
        $relative=[IO.Path]::GetRelativePath($linuxInput,$file.FullName).Replace('\','/')
        if($relative -match '(^|/)[^/]*(BackUpThisFolder_ButDontShipItWithYourGame|BurstDebugInformation_DoNotShip)[^/]*(/|$)'){continue}
        $entry=[System.Formats.Tar.PaxTarEntry]::new([System.Formats.Tar.TarEntryType]::RegularFile,'TrajetoInclusivo-Linux-x86_64/'+$relative)
        $entry.Mode=[IO.UnixFileMode]420
        if($relative -match '(\.sh$|\.x86_64$|\.so$)' -or $relative -eq 'UnityCrashHandler64'){$entry.Mode=[IO.UnixFileMode]493}
        $entry.Uid=0;$entry.Gid=0;$entry.ModificationTime=$file.LastWriteTimeUtc
        $entry.DataStream=[IO.File]::OpenRead($file.FullName)
        try {$tarWriter.WriteEntry($entry)} finally {$entry.DataStream.Dispose()}
    }
} finally {$tarWriter.Dispose();$gzipStream.Dispose();$archiveStream.Dispose()}
$archiveDigest=(Get-FileHash -LiteralPath $linuxArchive -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'SHA256SUMS.txt'),$archiveDigest+'  '+[IO.Path]::GetFileName($linuxArchive)+"`n",[Text.UTF8Encoding]::new($false))
Get-Item -LiteralPath $linuxArchive | Select-Object Name,Length
