using System;
using System.Reflection;
using System.Collections.Generic;

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

            AdvanceStep(burnKg);
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
        else if (!double.TryParse(input, out burnKg) || double.IsNaN(burnKg) || double.IsInfinity(burnKg) || burnKg < 0)
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

        ColoredConsole.Telemetry($"Scheduled burn: {burnKg:F2} kg");
        return burnKg;
    }

    // Upward is positive. The requested fuel is spread uniformly over the full step.
    internal void AdvanceStep(double burnKg)
    {
        double duration = cfg.TimeStep;
        double initialMass = state.CurrentMass;
        double flow = Math.Min(Math.Max(0, burnKg),
            Math.Min(state.FuelMass, cfg.MaxBurnRateKgPerSec * duration)) / duration;
        double altitude = state.Altitude;
        double velocity = state.Velocity;
        Func<double, double> speed = t => velocity + cfg.Gravity * t
            + (flow > 0 ? cfg.ExhaustVelocity * LogMassRatio(flow * t / initialMass) : 0);
        Func<double, double> height = t => altitude + velocity * t + 0.5 * cfg.Gravity * t * t
            + (flow > 0 ? cfg.ExhaustVelocity * BurnDistance(t, flow / initialMass) : 0);

        // Acceleration increases during a burn. Splitting at its zero and then
        // at velocity zeros gives monotonic height intervals, including hidden contact.
        var velocityIntervals = new List<double> { 0 };
        if (flow > 0 && cfg.Gravity < 0)
        {
            double turn = initialMass / flow + cfg.ExhaustVelocity / cfg.Gravity;
            if (turn > 0 && turn < duration) velocityIntervals.Add(turn);
        }
        velocityIntervals.Add(duration);
        var heightIntervals = new List<double> { 0 };
        for (int i = 1; i < velocityIntervals.Count; i++)
        {
            double left = velocityIntervals[i - 1], right = velocityIntervals[i];
            if (speed(left) * speed(right) < 0)
                heightIntervals.Add(FindZero(speed, left, right));
            heightIntervals.Add(right);
        }

        double elapsed = duration;
        bool landed = altitude <= 0;
        if (landed) elapsed = 0;
        for (int i = 1; !landed && i < heightIntervals.Count; i++)
        {
            double right = heightIntervals[i];
            if (height(right) <= 0)
            {
                elapsed = FindZero(height, heightIntervals[i - 1], right);
                landed = true;
            }
        }

        double consumed = flow * elapsed;
        state.Altitude = landed ? 0 : height(elapsed);
        state.Velocity = speed(elapsed);
        state.FuelMass = Math.Max(0, state.FuelMass - consumed);
        ColoredConsole.Telemetry($"Burned: {consumed:F2} kg");

        // Ideal continuous-exhaust energy budget: sum infinitesimal action-reaction
        // interactions at constant relative exhaust velocity (0.5 * fuel mass * ve^2).
        if (consumed > 0)
        {
            double interactionEnergy = 0.5 * consumed
                * cfg.ExhaustVelocity * cfg.ExhaustVelocity;
            state.TotalInteractionEnergyDelivered += interactionEnergy;
            ColoredConsole.Info($"Interaction Energy this burn (Vis Viva): {interactionEnergy:E10} J");
        }
        double remainingFuelInteractionEnergy = 0.5 * state.FuelMass
            * cfg.ExhaustVelocity * cfg.ExhaustVelocity;
        ColoredConsole.Info($"Remaining Fuel Interaction Energy: {remainingFuelInteractionEnergy:E10} J");
    }

    static double FindZero(Func<double, double> value, double left, double right)
    {
        if (value(left) == 0) return left;
        if (value(right) == 0) return right;
        double leftValue = value(left);
        for (int i = 0; i < 100; i++)
        {
            double middle = left + (right - left) / 2;
            if (middle == left || middle == right) break;
            double middleValue = value(middle);
            if (middleValue == 0) return middle;
            if ((middleValue > 0) == (leftValue > 0))
            {
                left = middle;
                leftValue = middleValue;
            }
            else right = middle;
        }
        return left + (right - left) / 2;
    }

    static double LogMassRatio(double fraction)
    {
        if (fraction >= 0.001) return -Math.Log(1 - fraction);
        double sum = 0, power = fraction;
        for (int n = 1; n <= 12; n++, power *= fraction) sum += power / n;
        return sum;
    }

    static double BurnDistance(double time, double fractionalFlow)
    {
        double fraction = fractionalFlow * time;
        // Series avoids cancellation in t - (m/q - t) * log(m/(m-q*t)).
        if (fraction < 0.001)
        {
            double sum = 0, power = fraction;
            for (int n = 1; n <= 12; n++, power *= fraction)
                sum += power / (n * (n + 1));
            return time * sum;
        }
        return time - (1 / fractionalFlow - time) * LogMassRatio(fraction);
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
