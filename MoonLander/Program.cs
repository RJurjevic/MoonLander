using System;

class MoonLander
{
    static void Main()
    {
        ColoredConsole.Init();

        var cfg = new LanderConfig
        {
            InitialAltitude = 500.0,
            InitialVelocity = -50.0,
            LanderMass = 1000.0,
            DryMass = 800.0,
            ExhaustVelocity = 2500.0,
            Gravity = -1.62,
            TimeStep = 1.0
        };

        if (cfg.InitialVelocity > 0)
        {
            ColoredConsole.Error("Initial velocity must be downward or zero for valid simulation.");
            return;
        }

        var sim = new MoonLanderSimulation(cfg);
        sim.Run();
    }
}
