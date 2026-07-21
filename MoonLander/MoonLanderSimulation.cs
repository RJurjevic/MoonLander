using System;
using System.Reflection;

class LanderConfig
{
    public double InitialAltitude { get; set; }
    public double InitialVelocity { get; set; }
    public double LanderMass { get; set; }
    public double DryMass { get; set; }
    public double ExhaustVelocity { get; set; }
    public double Gravity { get; set; }
    public double TimeStep { get; set; }
    public double MaxBurnRateKgPerSec { get; set; } // kg/s, engine's max propellant flow rate
}

class LanderState
{
    readonly LanderConfig cfg;

    public double Altitude { get; set; }
    public double Velocity { get; set; }
    public double FuelMass { get; set; }
    public double TotalInteractionEnergyDelivered { get; set; }

    public double CurrentMass => cfg.DryMass + FuelMass;
    public double MechanicalEnergy => CurrentMass * (0.5 * Velocity * Velocity + Math.Abs(cfg.Gravity) * Altitude);

    public LanderState(LanderConfig config)
    {
        cfg = config;
        Altitude = cfg.InitialAltitude;
        Velocity = cfg.InitialVelocity;
        FuelMass = cfg.LanderMass - cfg.DryMass;
        TotalInteractionEnergyDelivered = 0.0;
    }
}

class MoonLanderSimulation
{
    readonly LanderConfig cfg;
    readonly LanderState state;
    readonly double theoreticalFreeFallVelocity;

    public MoonLanderSimulation(LanderConfig config)
    {
        cfg = config;
        state = new LanderState(cfg);
        theoreticalFreeFallVelocity = Math.Sign(cfg.Gravity) * Math.Sqrt(cfg.InitialVelocity * cfg.InitialVelocity + 2 * Math.Abs(cfg.Gravity) * cfg.InitialAltitude);
    }

    public void Run()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        ColoredConsole.Header($"=== Vis Viva Moon Lander v{version} ===");
        ColoredConsole.Info("Try to land softly by applying thrust as fuel mass (kg).");

        while (state.Altitude > 0)
        {
            PrintStepTelemetry(state);

            double burnKg = ReadBurnKg(state);

            ApplyBurnAndEnergy(burnKg);

            StepOneSecondAndCorrectImpact();
        }

        PrintLandingReport();
    }

    void PrintStepTelemetry(LanderState s)
    {
        ColoredConsole.Telemetry($"\nAltitude: {s.Altitude:F2} m");
        ColoredConsole.Telemetry($"Velocity: {s.Velocity:F2} m/s");
        ColoredConsole.Telemetry($"Fuel: {s.FuelMass:F2} kg");

        ColoredConsole.Info($"Mechanical energy remaining: {s.MechanicalEnergy:E10} J");
    }

    double ReadBurnKg(LanderState s)
    {
        ColoredConsole.Prompt("Enter fuel to burn this step (kg): ");
        string input = Console.ReadLine();
        double burnKg;

        if (string.IsNullOrWhiteSpace(input))
        {
            ColoredConsole.Warn("Burn skipped.");
            burnKg = 0;
        }
        else if (!double.TryParse(input, out burnKg))
        {
            ColoredConsole.Error("Invalid input. Burn skipped.");
            burnKg = 0;
        }
        else
        {
            double maxThisStep = Math.Min(cfg.MaxBurnRateKgPerSec * cfg.TimeStep, s.FuelMass);
            if (burnKg > maxThisStep)
            {
                ColoredConsole.Warn($"Requested {burnKg:F2} kg exceeds what's deliverable this step. Capped to {maxThisStep:F2} kg.");
                burnKg = maxThisStep;
            }
        }

        ColoredConsole.Telemetry($"Burned: {burnKg:F2} kg");
        return burnKg;
    }

    void ApplyBurnAndEnergy(double burnKg)
    {
        double initialMass = cfg.DryMass + state.FuelMass;
        state.FuelMass -= burnKg;
        double finalMass = cfg.DryMass + state.FuelMass;
        double deltaV = burnKg > 0 ? cfg.ExhaustVelocity * Math.Log(initialMass / finalMass) : 0;

        if (burnKg > 0)
        {
            double interactionEnergy = 0.5 * (finalMass * burnKg) / (finalMass + burnKg) * cfg.ExhaustVelocity * cfg.ExhaustVelocity;
            state.TotalInteractionEnergyDelivered += interactionEnergy;
            ColoredConsole.Info($"Interaction Energy this burn (Vis Viva): {interactionEnergy:E10} J");
        }

        double currentMass = cfg.DryMass + state.FuelMass;
        double remainingFuelInteractionEnergy = 0.5 * (currentMass * state.FuelMass) / (currentMass + state.FuelMass) * cfg.ExhaustVelocity * cfg.ExhaustVelocity;
        ColoredConsole.Info($"Remaining Fuel Interaction Energy: {remainingFuelInteractionEnergy:E10} J");

        // Apply instantaneous delta-v
        state.Velocity += deltaV;
    }

    void StepOneSecondAndCorrectImpact()
    {
        double prevAltitude = state.Altitude;
        double prevVelocity = state.Velocity;

        state.Altitude += state.Velocity * cfg.TimeStep + 0.5 * cfg.Gravity * cfg.TimeStep * cfg.TimeStep;
        state.Velocity += cfg.Gravity * cfg.TimeStep;

        if (state.Altitude < 0)
        {
            CorrectImpactIfNeeded(prevAltitude, prevVelocity);
        }

        if (state.Altitude < 0) state.Altitude = 0;
    }

    void CorrectImpactIfNeeded(double prevAltitude, double prevVelocity)
    {
        double a = 0.5 * cfg.Gravity;
        double b = prevVelocity;
        double c = prevAltitude;

        double discriminant = b * b - 4 * a * c;

        if (discriminant >= 0)
        {
            double sqrtD = Math.Sqrt(discriminant);
            double dtImpact = (-b - sqrtD) / (2 * a);
            double correctedImpactVelocity = prevVelocity + cfg.Gravity * dtImpact;

            state.Velocity = correctedImpactVelocity;
            state.Altitude = 0;
        }
    }

    void PrintLandingReport()
    {
        double finalLanderMass = cfg.DryMass + state.FuelMass;
        double moonMass = 7.35e22; // kg
        double exactImpactEnergy = 0.5 * (moonMass * finalLanderMass) / (moonMass + finalLanderMass) * state.Velocity * state.Velocity;
        double approxImpactEnergy = 0.5 * finalLanderMass * state.Velocity * state.Velocity;

        ColoredConsole.Header("\n--- LANDING ---");
        ColoredConsole.Telemetry($"Final velocity: {state.Velocity:F2} m/s");
        if (state.Velocity >= -5) ColoredConsole.Success("SAFE landing."); else ColoredConsole.Error("CRASH landing.");
        ColoredConsole.Info($"Theoretical Free-Fall Velocity: {theoreticalFreeFallVelocity:F2} m/s");
        ColoredConsole.Info($"Exact Collision Energy (Vis Viva): {exactImpactEnergy:E10} J");
        ColoredConsole.Info($"Approximate Collision Energy (M ≫ m): {approxImpactEnergy:E10} J");
        ColoredConsole.Info($"Total Interaction Energy Delivered: {state.TotalInteractionEnergyDelivered:E10} J");
    }
}
