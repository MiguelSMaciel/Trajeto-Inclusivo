param(
    [string]$InputPath = (Join-Path $PSScriptRoot '../Assets/TrajetoInclusivo/Data/Geography/jucutuquara.osm'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '../Assets/TrajetoInclusivo/Data/Geography'),
    [double[]]$Crop = @(-40.3221, -20.3125, -40.3166, -20.3083),
    [string]$DownloadedAtUtc = '2026-10-15T14:33:00Z',
    [string]$SourceUrl = 'https://api.openstreetmap.org/api/0.6/map?bbox=-40.324,-20.314,-40.314,-20.305',
    [switch]$IncludeFaesa
)
$ErrorActionPreference = 'Stop'
$culture = [Globalization.CultureInfo]::InvariantCulture
[xml]$document = Get-Content -LiteralPath $InputPath -Raw
$nodeIndex = @{}
foreach ($n in $document.osm.node) { $nodeIndex[[string]$n.id] = $n }
function Get-Tags($element) {
    $tags = [ordered]@{}
    foreach ($tag in $element.tag) { $tags[[string]$tag.k] = [string]$tag.v }
    return $tags
}
function Get-Coordinate($id) {
    $n = $nodeIndex[[string]$id]
    if ($null -eq $n) { throw "Source node missing: $id" }
    return ,@([double]::Parse($n.lon, $culture), [double]::Parse($n.lat, $culture))
}
# The extraction rectangle is a crop, not a claim about real street dimensions.
if ($Crop.Count -ne 4 -or $Crop[0] -ge $Crop[2] -or $Crop[1] -ge $Crop[3]) { throw 'Invalid crop west,south,east,north' }
$bounds = $Crop
function Test-Inside($p) { return $p[0] -ge $bounds[0] -and $p[0] -le $bounds[2] -and $p[1] -ge $bounds[1] -and $p[1] -le $bounds[3] }
function Clip-Edge($a, $b) {
    $lo = 0.0; $hi = 1.0
    $dx = $b[0] - $a[0]; $dy = $b[1] - $a[1]
    $p = @((-$dx), $dx, (-$dy), $dy)
    $q = @(($a[0]-$bounds[0]), ($bounds[2]-$a[0]), ($a[1]-$bounds[1]), ($bounds[3]-$a[1]))
    for ($i = 0; $i -lt 4; $i++) {
        if ([Math]::Abs($p[$i]) -lt 1e-15) { if ($q[$i] -lt 0) { return $null }; continue }
        $r = $q[$i] / $p[$i]
        if ($p[$i] -lt 0) { $lo = [Math]::Max($lo, $r) } else { $hi = [Math]::Min($hi, $r) }
        if ($lo -gt $hi) { return $null }
    }
    return ,@($lo, $hi)
}
$features = [Collections.Generic.List[object]]::new()
$roadKinds = @('primary','secondary','tertiary','residential','unclassified','living_street','service','primary_link','secondary_link','tertiary_link','footway','pedestrian','path','steps')
$counts = [ordered]@{ road=0; building=0; park=0; campus=0; poi=0; skippedIncomplete=0 }
foreach ($way in $document.osm.way) {
    $tags = Get-Tags $way
    $refs = @($way.nd | ForEach-Object { [string]$_.ref })
    if (@($refs | Where-Object { -not $nodeIndex.ContainsKey($_) }).Count -gt 0) { $counts.skippedIncomplete++; continue }
    $points = @($refs | ForEach-Object { Get-Coordinate $_ })
    $props = [ordered]@{ osm_id = "way/$($way.id)"; osm_version = [string]$way.version; osm_timestamp = [string]$way.timestamp }
    foreach ($key in $tags.Keys) { $props[$key] = $tags[$key] }
    if ($tags['highway'] -in $roadKinds -and $tags['area'] -ne 'yes') {
        # Each source edge retains its real source node IDs. Cut endpoints receive unique IDs;
        # nearby or geometrically intersecting roads are never joined by coordinate proximity.
        for ($i = 0; $i -lt $points.Count - 1; $i++) {
            $a = $points[$i]; $b = $points[$i+1]; $clip = Clip-Edge $a $b
            if ($null -eq $clip -or $clip[1] - $clip[0] -lt 1e-9) { continue }
            $start = @(($a[0] + ($b[0]-$a[0])*$clip[0]), ($a[1] + ($b[1]-$a[1])*$clip[0]))
            $end = @(($a[0] + ($b[0]-$a[0])*$clip[1]), ($a[1] + ($b[1]-$a[1])*$clip[1]))
            $copy = [ordered]@{}
            foreach ($key in $props.Keys) { $copy[$key] = $props[$key] }
            $copy['kind'] = 'road'
            $startId = if ($clip[0] -lt 1e-9) { $refs[$i] } else { "clip_$($way.id)_${i}_a" }
            $endId = if ($clip[1] -gt 1-1e-9) { $refs[$i+1] } else { "clip_$($way.id)_${i}_b" }
            $copy['node_refs'] = "$startId,$endId"
            $features.Add([ordered]@{ type='Feature'; id="way/$($way.id)/$i"; properties=$copy; geometry=[ordered]@{type='LineString';coordinates=@($start,$end)} })
            $counts.road++
        }
        continue
    }
    $kind = if ($tags.Contains('building') -and $tags['building'] -ne 'no') { 'building' }
        elseif ($way.id -eq '541652953') { 'park' }
        elseif ($way.id -eq '1352259307') { 'campus' } else { '' }
    if ($kind -eq '' -or $refs.Count -lt 4 -or $refs[0] -ne $refs[-1]) { continue }
    if (@($points | Where-Object { Test-Inside $_ }).Count -eq 0) { continue }
    $props['kind'] = $kind
    $features.Add([ordered]@{ type='Feature'; id="way/$($way.id)"; properties=$props; geometry=[ordered]@{type='Polygon';coordinates=(,@($points))} })
    $counts[$kind]++
}
foreach ($n in $document.osm.node) {
    $tags = Get-Tags $n
    if ($tags.Count -eq 0) { continue }
    $p = Get-Coordinate $n.id
    if (-not (Test-Inside $p)) { continue }
    if (-not ($tags.Contains('entrance') -or $tags['highway'] -eq 'crossing' -or $tags['barrier'] -eq 'gate')) { continue }
    $props = [ordered]@{ kind='poi'; osm_id="node/$($n.id)"; osm_version=[string]$n.version; osm_timestamp=[string]$n.timestamp }
    foreach ($key in $tags.Keys) { $props[$key] = $tags[$key] }
    $features.Add([ordered]@{type='Feature';id="node/$($n.id)";properties=$props;geometry=[ordered]@{type='Point';coordinates=$p}})
    $counts.poi++
}
$geojson = [ordered]@{type='FeatureCollection';name='Avenida Vitória — recorte OSM';attribution='© OpenStreetMap contributors';license='https://opendatacommons.org/licenses/odbl/1-0/';extractedAt=$DownloadedAtUtc;features=@($features.ToArray())}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$geojson | ConvertTo-Json -Depth 30 -Compress | Set-Content -LiteralPath (Join-Path $OutputDirectory 'avenida_vitoria.geojson.json') -Encoding utf8
$origin = Get-Coordinate '12511666338'
$metadata = [ordered]@{
    title='Avenida Vitória / Jucutuquara — OpenStreetMap'
    downloadedAtUtc=$DownloadedAtUtc
    sourceUrl=$SourceUrl
    sourceSha256=(Get-FileHash -LiteralPath $InputPath -Algorithm SHA256).Hash.ToLowerInvariant()
    sourceFile=[IO.Path]::GetFileName($InputPath)
    license='ODbL 1.0'
    attribution='© OpenStreetMap contributors'
    licenseUrl='https://www.openstreetmap.org/copyright'
    crs='EPSG:4326 (lon, lat); no vertical data'
    crop=@($bounds)
    origin=[ordered]@{osmNodeId='12511666338';longitude=$origin[0];latitude=$origin[1];altitude=$null}
    landmarks=@([ordered]@{osmId='way/541652953';name='Praça Asdrúbal Soares'},[ordered]@{osmId='way/1352259307';name='IFES - Campus VItória'})
    counts=$counts
    processing='Ways only. Roads clipped edge by edge; polygons retained whole when a vertex falls inside crop. Multipolygon relations not reconstructed. Original tags and versions retained. No widths, elevations or accessibility assessments inferred as verified.'
}
if($IncludeFaesa) {
    $metadata.title='Avenida Vitória — Praça / Ifes / FAESA — OpenStreetMap'
    $metadata.landmarks += [ordered]@{osmId='way/51374617';name='Edifício OSM no endereço da FAESA, Av. Vitória 2220';identitySource='https://www.faesa.br/contato/';derivation='Address match. Destination is a projection onto the approximate Avenida Vitória sidewalk; no verified campus entrance.'}
}
$metadata | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'provenance.json') -Encoding utf8
$counts | ConvertTo-Json -Compress
