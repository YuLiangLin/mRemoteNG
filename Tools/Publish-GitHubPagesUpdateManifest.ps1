#Requires -Version 7.0
param(
    [Parameter(Mandatory)]
    [string]$RepositoryOwner,

    [Parameter(Mandatory)]
    [string]$RepositoryName,

    [Parameter(Mandatory)]
    [string]$TagName,

    [Parameter(Mandatory)]
    [string]$Version,

    [Parameter(Mandatory)]
    [string]$ReleaseAsset,

    [Parameter(Mandatory)]
    [string]$OutputDir
)

$ErrorActionPreference = "Stop"

$parsedVersion = $null
if (-not [System.Version]::TryParse($Version, [ref]$parsedVersion)) {
    throw "Version '$Version' is not a valid numeric System.Version value."
}

$asset = Get-Item -LiteralPath $ReleaseAsset
if ($asset.Name -ne "mRemoteNG.exe") {
    throw "The portable release asset must be named mRemoteNG.exe."
}

$ownerForHost = $RepositoryOwner.ToLowerInvariant()
$releaseUrl = "https://github.com/$RepositoryOwner/$RepositoryName/releases/tag/$TagName"
$downloadUrl = "https://github.com/$RepositoryOwner/$RepositoryName/releases/download/$TagName/$($asset.Name)"
$pagesBaseUrl = "https://$ownerForHost.github.io/$RepositoryName/"
$changelogUrl = "${pagesBaseUrl}changelog.txt"
$checksum = (Get-FileHash -LiteralPath $asset.FullName -Algorithm SHA512).Hash

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$manifest = @(
    "Version: $Version"
    "dURL: $downloadUrl"
    "clURL: $changelogUrl"
    "Checksum: $checksum"
) -join "`n"

Set-Content -LiteralPath (Join-Path $OutputDir "update-portable.txt") `
    -Value "$manifest`n" -NoNewline -Encoding utf8

$changelog = @"
mRemoteNG fork $TagName

Version: $Version
Release notes: $releaseUrl
Source code: https://github.com/$RepositoryOwner/$RepositoryName/tree/$TagName
"@

Set-Content -LiteralPath (Join-Path $OutputDir "changelog.txt") `
    -Value $changelog -Encoding utf8

$encodedTag = [System.Net.WebUtility]::HtmlEncode($TagName)
$encodedVersion = [System.Net.WebUtility]::HtmlEncode($Version)
$encodedReleaseUrl = [System.Net.WebUtility]::HtmlEncode($releaseUrl)
$encodedDownloadUrl = [System.Net.WebUtility]::HtmlEncode($downloadUrl)
$encodedChecksum = [System.Net.WebUtility]::HtmlEncode($checksum)

$indexHtml = @"
<!doctype html>
<html lang="zh-Hant">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>mRemoteNG Fork Updates</title>
  <style>
    body { max-width: 760px; margin: 48px auto; padding: 0 24px; font: 16px/1.6 system-ui, sans-serif; color: #172033; }
    a.button { display: inline-block; padding: 10px 16px; border-radius: 8px; background: #0969da; color: white; text-decoration: none; }
    code { overflow-wrap: anywhere; }
  </style>
</head>
<body>
  <h1>mRemoteNG Fork Updates</h1>
  <p>Latest release: <strong>$encodedTag</strong> (internal version $encodedVersion)</p>
  <p><a class="button" href="$encodedDownloadUrl">Download mRemoteNG.exe</a></p>
  <p><a href="$encodedReleaseUrl">Release notes and source code</a></p>
  <h2>SHA-512</h2>
  <code>$encodedChecksum</code>
</body>
</html>
"@

Set-Content -LiteralPath (Join-Path $OutputDir "index.html") `
    -Value $indexHtml -Encoding utf8
New-Item -ItemType File -Path (Join-Path $OutputDir ".nojekyll") -Force | Out-Null
