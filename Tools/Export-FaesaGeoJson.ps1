param([string]$DownloadedAtUtc='2026-10-15T16:00:51Z')
$ErrorActionPreference='Stop'
$faesaFolder=Join-Path $PSScriptRoot '../Assets/TrajetoInclusivo/Data/Geography/FAESA'
$faesaSource=Join-Path $faesaFolder 'avenida_vitoria_faesa.osm'
$faesaRetrieval=$DownloadedAtUtc
& (Join-Path $PSScriptRoot 'Export-OsmGeoJson.ps1') -InputPath $faesaSource -OutputDirectory $faesaFolder -Crop @(-40.3221,-20.3125,-40.313,-20.3083) -DownloadedAtUtc $faesaRetrieval -SourceUrl 'https://api.openstreetmap.org/api/0.6/map?bbox=-40.323,-20.314,-40.308,-20.307' -IncludeFaesa
