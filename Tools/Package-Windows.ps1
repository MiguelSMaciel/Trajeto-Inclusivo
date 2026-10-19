param(
    [string]$InputDirectory=(Join-Path $PSScriptRoot '../Builds/Windows'),
    [string]$OutputDirectory=(Join-Path $PSScriptRoot '../Distribution')
)
$ErrorActionPreference='Stop'
$projectRoot=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$windowsInput=(Resolve-Path -LiteralPath $InputDirectory).Path
foreach($required in 'TrajetoInclusivo.exe','UnityPlayer.dll','TrajetoInclusivo_Data','MonoBleedingEdge'){
    if(-not(Test-Path -LiteralPath (Join-Path $windowsInput $required))){throw "Arquivo/pasta necessário ausente: $required"}
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$windowsOutput=(Resolve-Path -LiteralPath $OutputDirectory).Path
if($windowsOutput.Equals($windowsInput,[StringComparison]::OrdinalIgnoreCase) -or $windowsOutput.StartsWith($windowsInput+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){
    throw 'A pasta de saída deve ficar fora da pasta do build.'
}
$documentation=Join-Path $windowsInput 'Documentacao'
New-Item -ItemType Directory -Path $documentation -Force | Out-Null
Copy-Item -Path (Join-Path $projectRoot 'Docs/*') -Destination $documentation -Recurse -Force
$readme=[IO.File]::ReadAllText((Join-Path $projectRoot 'README.md'))
[IO.File]::WriteAllText((Join-Path $windowsInput 'README.md'),$readme.Replace('](Docs/','](Documentacao/'))
[IO.File]::WriteAllText((Join-Path $documentation 'README.md'),$readme.Replace('](Docs/',']('))
$notice=[IO.File]::ReadAllText((Join-Path $projectRoot 'NOTICE.md'))
[IO.File]::WriteAllText((Join-Path $windowsInput 'NOTICE.md'),$notice.Replace('](Docs/','](Documentacao/'))
[IO.File]::WriteAllText((Join-Path $windowsInput 'LEIA-ME-WINDOWS.txt'),"Trajeto Inclusivo — Windows x86_64`n`nExtraia toda a pasta antes de executar TrajetoInclusivo.exe.`nMantenha as DLLs, MonoBleedingEdge e TrajetoInclusivo_Data junto do executável.`nOs guias e resultados estão em Documentacao; dados e créditos em MapData.`n")
$windowsArchive=Join-Path $windowsOutput 'TrajetoInclusivo-Windows-x86_64.zip'
$archiveStream=[IO.File]::Create($windowsArchive)
$zipWriter=[IO.Compression.ZipArchive]::new($archiveStream,[IO.Compression.ZipArchiveMode]::Create,$false)
try {
    foreach($file in Get-ChildItem -LiteralPath $windowsInput -File -Recurse | Sort-Object FullName){
        $relative=[IO.Path]::GetRelativePath($windowsInput,$file.FullName).Replace('\','/')
        if($relative -match '(^|/)[^/]*(BackUpThisFolder_ButDontShipItWithYourGame|BurstDebugInformation_DoNotShip)[^/]*(/|$)'){continue}
        $entry=$zipWriter.CreateEntry('TrajetoInclusivo-Windows-x86_64/'+$relative,[IO.Compression.CompressionLevel]::Optimal)
        $entry.LastWriteTime=$file.LastWriteTimeUtc
        $inputStream=[IO.File]::OpenRead($file.FullName)
        $entryStream=$entry.Open()
        try {$inputStream.CopyTo($entryStream)} finally {$entryStream.Dispose();$inputStream.Dispose()}
    }
} finally {$zipWriter.Dispose();$archiveStream.Dispose()}
$digest=(Get-FileHash -LiteralPath $windowsArchive -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $windowsOutput 'SHA256SUMS-Windows.txt'),$digest+'  '+[IO.Path]::GetFileName($windowsArchive)+"`n",[Text.UTF8Encoding]::new($false))
Get-Item -LiteralPath $windowsArchive | Select-Object Name,Length
