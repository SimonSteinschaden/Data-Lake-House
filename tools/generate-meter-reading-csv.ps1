param(
    [Parameter(Mandatory = $true)]
    [ValidateRange(1, 100000000)]
    [int]$Rows,
    [string]$MeterNumber = "AT001000000000000001",
    [datetime]$StartUtc = [datetime]"2025-01-01T00:00:00Z",
    [ValidateRange(1, 86400)]
    [int]$IntervalSeconds = 900,
    [string]$Unit = "KWh",
    [string]$ReadingType = "IntervalValue",
    [string]$QualityFlag = "Measured",
    [ValidateRange(0, 1)]
    [double]$ErrorRate = 0,
    [ValidateRange(0, 1)]
    [double]$DuplicateRate = 0,
    [string]$OutputPath = "meter-readings.generated.csv"
)

$targetPath = [System.IO.Path]::GetFullPath($OutputPath)
$utf8 = [System.Text.UTF8Encoding]::new($true)
$writer = [System.IO.StreamWriter]::new($targetPath, $false, $utf8, 1048576)
$random = [System.Random]::new(20260730)
try {
    $writer.WriteLine(
        "MeterNumber;Timestamp;Value;Unit;ReadingType;QualityFlag;IntervalSeconds")
    $previous = $null
    for ($index = 0; $index -lt $Rows; $index++) {
        if ($previous -and $random.NextDouble() -lt $DuplicateRate) {
            $line = $previous
        }
        else {
            $timestamp = $StartUtc.ToUniversalTime().AddSeconds(
                [long]$index * $IntervalSeconds)
            $value = [math]::Round(10 + (($index % 1000) / 100.0), 3)
            $valueText = $value.ToString(
                [System.Globalization.CultureInfo]::InvariantCulture)
            if ($random.NextDouble() -lt $ErrorRate) {
                $valueText = "INVALID"
            }
            $line = "{0};{1};{2};{3};{4};{5};{6}" -f `
                $MeterNumber,
                $timestamp.ToString("O"),
                $valueText,
                $Unit,
                $ReadingType,
                $QualityFlag,
                $IntervalSeconds
            $previous = $line
        }
        $writer.WriteLine($line)
    }
}
finally {
    $writer.Dispose()
}
Write-Output $targetPath
