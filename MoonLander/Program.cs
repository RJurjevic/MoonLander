using System;

class MoonLander
{
    static void Main()
    {
        double initialAltitude = 500.0; 
        double altitude = initialAltitude;
        double initialVelocity = -50.0;  // m/s (downward or stationary only — must not be positive)
        double velocity = initialVelocity;  
        double landerMass = 1000.0;      // kg (initial mass including fuel)
        double dryMass = 800.0;          // kg (lander without fuel)
        double fuelMass = landerMass - dryMass;
        double exhaustVelocity = 2500.0; // m/s (typical chemical engine)
        double gravity = -1.62;          // Moon surface gravity, m/s²
        double theoreticalFreeFallVelocity = Math.Sign(gravity) * Math.Sqrt(initialVelocity * initialVelocity + 2 * Math.Abs(gravity) * initialAltitude);
        double timeStep = 1.0;           // seconds 
        double burnKg;
        double totalInteractionEnergyDelivered = 0.0;

        if (initialVelocity > 0)
        {
            Console.WriteLine("ERROR: Initial velocity must be downward or zero for valid simulation.");
            return;
        }

        Console.WriteLine("=== Vis Viva Moon Lander ===");
        Console.WriteLine("Try to land softly by applying thrust as fuel mass (kg).");

        while (altitude > 0)
        {
            Console.WriteLine($"\nAltitude: {altitude:F2} m");
            Console.WriteLine($"Velocity: {velocity:F2} m/s");
            Console.WriteLine($"Fuel: {fuelMass:F2} kg");

            Console.Write("Enter fuel to burn this step (kg): ");
            string input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Burn skipped.");
                burnKg = 0;  // Default to 0 fuel burned
            }
            else if (!double.TryParse(input, out burnKg))
            {
                Console.WriteLine("Invalid input. Burn skipped.");
                burnKg = 0;
            }
            if (burnKg > fuelMass)
            {
                Console.WriteLine("Not enough fuel. Burn skipped.");
                burnKg = 0;
            }

            double initialMass = dryMass + fuelMass;
            fuelMass -= burnKg;
            double finalMass = dryMass + fuelMass;
            double deltaV = burnKg > 0 ? exhaustVelocity * Math.Log(initialMass / finalMass) : 0;

            // Calculate Vis Viva interaction energy
            if (burnKg > 0)
            {
                double interactionEnergy = 0.5 * (finalMass * burnKg) / (finalMass + burnKg) * exhaustVelocity * exhaustVelocity;
                totalInteractionEnergyDelivered += interactionEnergy;
                Console.WriteLine($"Interaction Energy this burn (Vis Viva): {interactionEnergy:E10} J");
            }

            double currentMass = dryMass + fuelMass;
            double remainingFuelInteractionEnergy =
                0.5 * (currentMass * fuelMass) / (currentMass + fuelMass) * exhaustVelocity * exhaustVelocity;
            Console.WriteLine($"Remaining Fuel Interaction Energy: {remainingFuelInteractionEnergy:E10} J");


            double prevAltitude = altitude;
            double prevVelocity = velocity;

            // Apply instantaneous delta-v from thrust (assumes burn is rapid compared to time step)
            velocity += deltaV;

            altitude += velocity * timeStep + 0.5 * gravity * timeStep * timeStep;
            velocity += gravity * timeStep;
 
            if (altitude < 0)
            {
                // Solve for time to hit the surface during last step
                double a = 0.5 * gravity;
                double b = prevVelocity;
                double c = prevAltitude;

                double discriminant = b * b - 4 * a * c;

                if (discriminant >= 0)
                {
                    double sqrtD = Math.Sqrt(discriminant);
                    double dtImpact = (-b - sqrtD) / (2 * a);  // take the smaller root

                    double correctedImpactVelocity = prevVelocity + gravity * dtImpact;

                    // Apply the corrected values
                    velocity = correctedImpactVelocity;
                    altitude = 0;
                }
            }
 
            if (altitude < 0) altitude = 0;
        }

        double finalLanderMass = dryMass + fuelMass;
        double moonMass = 7.35e22; // kg
        double exactImpactEnergy = 0.5 * (moonMass * finalLanderMass) / (moonMass + finalLanderMass) * velocity * velocity;
        double approxImpactEnergy = 0.5 * finalLanderMass * velocity * velocity;

        Console.WriteLine("\n--- LANDING ---");
        Console.WriteLine($"Final velocity: {velocity:F2} m/s");
        Console.WriteLine(velocity >= -5 ? "Safe landing." : "CRASH landing.");
        Console.WriteLine($"Theoretical Free-Fall Velocity: {theoreticalFreeFallVelocity:F2} m/s");
        Console.WriteLine($"Exact Collision Energy (Vis Viva): {exactImpactEnergy:E10} J");
        Console.WriteLine($"Approximate Collision Energy (M ≫ m): {approxImpactEnergy:E10} J");
        Console.WriteLine($"Total Interaction Energy Delivered: {totalInteractionEnergyDelivered:E10} J");
    }
}
