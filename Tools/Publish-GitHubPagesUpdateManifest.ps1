#Requires -Version 6.0
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
    [string]$Build,

    [Parameter(Mandatory)]
    [string]$OutputDir,

    [ValidateSet("Stable","Preview","Nightly")]
    [string]$Channel = "Stable",

    [ValidateSet("False","True")]
    [string]$Publish = "False"
)

$ErrorActionPreference = "Stop"

if ($IsWindows -ne $true) {
    throw "This script only supports Windows PowerShell/Core runners."
}

if (-not $env:GITHUB_TOKEN) {
    throw "GITHUB_TOKEN is required to read release assets."
}

$headers = @{
    Authorization = "Bearer $env:GITHUB_TOKEN"
    "User-Agent" = "mRemoteNG-workflow"
    Accept = "application/vnd.github+json"
}

function New-UpdateFileContent {
    param(
        [string]$Version,
        [string]$DownloadUrl,
        [string]$ChangelogUrl,
        [string]$Checksum,
        [string]$CertificateThumbprint
    )

    $lines = @(
        "Version: $Version"
        "dURL: $DownloadUrl"
        "clURL: $ChangelogUrl"
        "Checksum: $Checksum"
    )

    if ($PSBoundParameters.ContainsKey("CertificateThumbprint") -and -not [string]::IsNullOrWhiteSpace($CertificateThumbprint)) {
        $lines = @(
            $lines[0],
            $lines[1],
            $lines[2],
            "CertificateThumbprint: $CertificateThumbprint",
            $lines[3]
        )
    }

    return ($lines -join "`r`n") + "`r`n"
}

function Get-AssetByExtension {
    param(
        [Parameter(Mandatory)]
        [array]$Assets,
        [Parameter(Mandatory)]
        [string]$Extension
    )

    return $Assets |
        Where-Object { $_.name -like "*$Extension" -and $_.name -notlike "*-symbols-*.zip" } |
        Sort-Object { [datetime]$_.created_at } -Descending |
        Select-Object -First 1
}

Write-Host "Load release metadata for tag: $TagName"
$encodedTag = [uri]::EscapeDataString($TagName)
$releaseUrl = "https://api.github.com/repos/$RepositoryOwner/$RepositoryName/releases/tags/$encodedTag"
$release = Invoke-RestMethod -Uri $releaseUrl -Headers $headers
if ($null -eq $release) {
    throw "Could not resolve release: $releaseUrl"
}

if ($release.assets.Count -eq 0) {
    throw "No release assets found for $RepositoryOwner/$RepositoryName@$TagName."
}

$fullVersion = "$Version.$Build"
$changelogUrl = "https://raw.githubusercontent.com/$RepositoryOwner/$RepositoryName/$encodedTag/CHANGELOG.md"
$tmpDir = Join-Path $env:TEMP "mremote-update-$([guid]::NewGuid())"
New-Item -ItemType Directory -Path $tmpDir | Out-Null

try {
    $portableAsset = Get-AssetByExtension -Assets $release.assets -Extension ".zip"
    $normalAsset = Get-AssetByExtension -Assets $release.assets -Extension ".msi"

    $publishItems = @()

    if ($null -ne $portableAsset) {
        Write-Host "Generate portable update check file from $($portableAsset.name)"
        gh release download $TagName --repo "$RepositoryOwner/$RepositoryName" --pattern $portableAsset.name --dir $tmpDir
        $portablePath = Join-Path $tmpDir $portableAsset.name
        $portableHash = (Get-FileHash -Path $portablePath -Algorithm SHA512).Hash

        $portableContent = New-UpdateFileContent -Version $fullVersion -DownloadUrl $portableAsset.browser_download_url -ChangelogUrl $changelogUrl -Checksum $portableHash

        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

        $portableFile = Join-Path $OutputDir "update-portable.txt"
        if ($Channel -eq "Preview") { $portableFile = Join-Path $OutputDir "preview-update-portable.txt" }
        elseif ($Channel -eq "Nightly") { $portableFile = Join-Path $OutputDir "nightly-update-portable.txt" }

        Set-Content -Path $portableFile -Value $portableContent -NoNewline -Encoding UTF8
        $publishItems += $portableFile
    }

    if ($null -ne $normalAsset) {
        Write-Host "Generate normal update check file from $($normalAsset.name)"
        gh release download $TagName --repo "$RepositoryOwner/$RepositoryName" --pattern $normalAsset.name --dir $tmpDir
        $normalPath = Join-Path $tmpDir $normalAsset.name
        $normalHash = (Get-FileHash -Path $normalPath -Algorithm SHA512).Hash
        $thumbprint = $null
        try {
            $signature = Get-AuthenticodeSignature -FilePath $normalPath
            if ($signature.Status -eq "Valid") {
                $thumbprint = $signature.SignerCertificate.Thumbprint
            }
        }
        catch {
            Write-Host "Warning: unable to read Authenticode signature for $($normalAsset.name), will omit CertificateThumbprint."
        }

        $normalContent = New-UpdateFileContent -Version $fullVersion -DownloadUrl $normalAsset.browser_download_url -ChangelogUrl $changelogUrl -Checksum $normalHash -CertificateThumbprint $thumbprint

        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

        $normalFile = Join-Path $OutputDir "update.txt"
        if ($Channel -eq "Preview") { $normalFile = Join-Path $OutputDir "preview-update.txt" }
        elseif ($Channel -eq "Nightly") { $normalFile = Join-Path $OutputDir "nightly-update.txt" }

        Set-Content -Path $normalFile -Value $normalContent -NoNewline -Encoding UTF8
        $publishItems += $normalFile
    }

    if ($publishItems.Count -eq 0) {
        throw "No supported release assets (.zip or .msi) found for tag $TagName."
    }

    if ($Publish -eq "True") {
        gh release view $TagName --repo "$RepositoryOwner/$RepositoryName" --json body | Out-Null
        Write-Host "Manifest files:"
        $publishItems | ForEach-Object { Write-Host " - $_" }
    }
}
finally {
    if (Test-Path $tmpDir) {
        Remove-Item -Recurse -Force $tmpDir
    }
}
