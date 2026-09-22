param(
    [Parameter(Mandatory = $true)]
    [string]$Path
)

$resolved = (Resolve-Path -LiteralPath $Path).Path
$lines = Get-Content -LiteralPath $resolved
$seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$output = [System.Collections.Generic.List[string]]::new()
$output.Add('address,name,evidence')

foreach ($line in ($lines | Select-Object -Skip 1)) {
    if ([string]::IsNullOrWhiteSpace($line)) {
        continue
    }

    $firstComma = $line.IndexOf(',')
    if ($firstComma -lt 1) {
        throw "Malformed provisional-name row: $line"
    }

    $address = $line.Substring(0, $firstComma).Trim().ToUpperInvariant()
    if ($address -notmatch '^0X[0-9A-F]{8}$') {
        throw "Invalid provisional-name address: $address"
    }

    if ($seen.Add($address)) {
        $output.Add($line)
    }
}

Set-Content -LiteralPath $resolved -Value $output -Encoding utf8
Write-Output "normalized=$resolved rows=$($output.Count - 1) removed=$($lines.Count - $output.Count)"
