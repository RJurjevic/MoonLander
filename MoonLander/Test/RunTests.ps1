$exe = Join-Path (Split-Path -Parent $PSScriptRoot) "bin\Release\MoonLander.exe"

if (-not (Test-Path $exe)) {
    throw "MoonLander.exe not found: $exe"
}

$checks = [ordered]@{
    "SafeLanding.txt" = @(
        "SAFE landing."
        "Final velocity: -2.78 m/s"
        "Fuel: 174.00 kg"
        "Total Interaction Energy Delivered: 8.1250000000E+007 J"
        "Remaining Fuel Interaction Energy: 5.4375000000E+008 J"
    )

    "HiddenImpact.txt" = @(
        "Burned: 4.96 kg"
        "Final velocity: -12.04 m/s"
        "CRASH landing."
    )

    "FreeFall.txt" = @(
        "Final velocity: -64.19 m/s"
        "Total Interaction Energy Delivered: 0.0000000000E+000 J"
    )

    "PartialStepImpact.txt" = @(
        "Burned: 9.24 kg"
        "Final velocity: -41.25 m/s"
    )

    "ContinuousBurn.txt" = @(
        "Altitude: 474.36 m"
        "Velocity: -1.11 m/s"
        "Final velocity: -39.22 m/s"
    )

    "NegativeBurn.txt" = @(
        "Invalid input. Burn skipped."
        "Fuel: 200.00 kg"
        "Final velocity: -64.19 m/s"
    )

    "InputValidation.txt" = @(
        "Capped to 20.00 kg."
        "Fuel: 180.00 kg"
        "Final velocity: -26.83 m/s"
    )

    "FuelDepletion.txt" = @(
        "Fuel: 0.00 kg"
        "Capped to 0.00 kg."
        "Final velocity: -500.17 m/s"
    )
}

$failed = 0

foreach ($name in $checks.Keys) {
    $file = Join-Path $PSScriptRoot $name

    $output = Get-Content $file | & $exe | Out-String

    $missing = @(
        $checks[$name] | Where-Object {
            -not $output.Contains($_)
        }
    )

    if ($name -eq "InputValidation.txt") {
        $count = [regex]::Matches(
            $output, "Invalid input\. Burn skipped\."
        ).Count

        if ($count -ne 5) {
            $missing += "Expected 5 rejected inputs; got $count"
        }
    }

    if ($name -eq "FuelDepletion.txt") {
        $count = [regex]::Matches(
            $output, "Burned: 20\.00 kg"
        ).Count

        if ($count -ne 10) {
            $missing += "Expected 10 full burns; got $count"
        }
    }

    if ($missing.Count -eq 0) {
        Write-Host "PASS  $name" -ForegroundColor Green
    }
    else {
        Write-Host "FAIL  $name" -ForegroundColor Red
        $missing | ForEach-Object { Write-Host "  Missing: $_" }
        $failed++
    }
}

if ($failed -gt 0) {
    throw "$failed regression test(s) failed."
}

Write-Host "All regression tests passed." -ForegroundColor Green
