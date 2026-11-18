# Vis Viva Moon Lander

A console-based Moon landing simulation inspired by the classic HP-34C programmable pocket calculator — now reimagined with modern C# and Vis Viva physics principles.

## 🧠 About the Project

This is a physics simulation program that models the descent of a lunar lander toward the Moon's surface under the influence of gravity and user-controlled thrust. Version **2.0.0.0** includes:

- **Continuous fuel ejection** at a constant rate within each step, with changing spacecraft mass
- **Analytical rocket equations** for altitude and velocity under constant gravity
- **First-contact touchdown detection**, with fuel charged only until contact
- **Live telemetry** for velocity, fuel, altitude, mechanical energy and ideal propulsion-energy diagnostics
- **Input validation**, engine fuel-flow limits and safe or crash landing outcomes
- **Eight automated regression tests** covering landing, propulsion and input handling

The program is a modern interpretation of a well-known simulation originally coded into HP's HP-34C calculator — a programmable pocket calculator from the late 1970s. This updated version is written in C# and is built to run in the Windows Terminal or standard console.

## ⚙️ Physics Behind the Simulation

The project retains its **Vis Viva** conceptual motivation: examining fuel ejection and landing through the energy associated with interactions. The implemented trajectory model is Newtonian, one-dimensional and uses constant lunar gravity. It does not calculate curved space-time or relativistic geodesics.

### Continuous propulsion and analytical motion

Upward velocity is positive. Each input specifies kilograms of fuel to distribute uniformly over the configured time step, rather than an instantaneous impulse. With initial mass `m0`, constant fuel flow `q`, relative exhaust speed `ve`, signed gravity `g` and elapsed time `t` within the step:

```text
m(t) = m0 - q*t
v(t) = v0 + g*t + ve*ln(m0 / m(t))
h(t) = h0 + v0*t + 0.5*g*t^2
       + ve*[t - (m0/q - t)*ln(m0 / m(t))]    (q > 0)
```

For zero fuel flow, the simulation uses the ballistic equations `v(t) = v0 + g*t` and `h(t) = h0 + v0*t + 0.5*g*t^2`. Small-burn series evaluations reduce cancellation when very little fuel is consumed.

These are analytical solutions for the assumed constant flow and gravity within each step, evaluated with floating-point arithmetic. The player may choose a new flow rate at the next step. The default step is one second.

### First-contact touchdown

The simulation searches for the earliest surface contact within a step. It splits the trajectory at acceleration and velocity turning points to obtain monotonic altitude intervals, then uses a bracketed numerical root search where contact occurs.

This catches **hidden impacts**: a trajectory may touch or cross the surface and then rise above it again before the end of the step. Checking only the final altitude would miss that contact.

At touchdown, altitude is set to zero and velocity is evaluated at the contact time. Only `q*t_contact` kilograms of fuel are consumed in that final step; any fuel scheduled for later in the step remains aboard. `Scheduled burn` and `Burned` therefore need not match.

For a downward touchdown, velocities of **−5 m/s or gentler** receive the program's `SAFE landing.` classification; faster descents receive `CRASH landing.` This is a gameplay threshold, not a structural landing analysis.

### Energy diagnostics

The console retains the Vis Viva diagnostic labels. For continuous fuel ejection at constant relative exhaust speed, the accumulated ideal interaction-energy budget is:

```text
Interaction energy this burn = 0.5 * consumedFuelMass * ve^2
Total interaction energy delivered = sum of interaction energy over all burns
Remaining fuel interaction energy = 0.5 * remainingFuelMass * ve^2
```

These formulas sum infinitesimal action–reaction interactions. They assume ideal continuous exhaust with constant relative speed and omit engine inefficiency, thermal losses and other propulsion details. They describe a propulsion-energy budget, not a finite two-body separation and not the increase in the lander's mechanical energy. The remaining-fuel value is the ideal budget associated with ejecting that fuel at the configured exhaust speed; it is not a model of the fuel's chemical energy or an amount necessarily transferred to the spacecraft.

With the default 200 kg of initial fuel and 2500 m/s exhaust speed, delivered plus remaining interaction energy is **625 MJ**, up to floating-point rounding.

The separate mechanical-energy readout is `m*(0.5*v^2 + |g|*h)`, using the lander's current mass and the surface as the potential-energy reference. During an unpowered segment it is conserved within floating-point accuracy. During propulsion, mass and velocity change continuously; this readout is not a closed-system energy balance including the exhaust.

Moon–lander collision-energy calculations remain:

```text
Reduced mass: mu = M*m / (M + m)
Exact Collision Energy (Vis Viva) = 0.5 * mu * v_impact^2
Approximate Collision Energy (M ≫ m) = 0.5 * m * v_impact^2
```

Here `M = 7.35e22 kg` is the Moon's mass and `m` is the lander's mass at touchdown. The exact label refers to the reduced-mass expression within this ideal two-body model. It does not model surface deformation, rebound or how collision energy is distributed.

## 💻 How to Run

### Option 1: Precompiled Release

Visit [GitHub Releases](https://github.com/RJurjevic/MoonLander/releases) for published versions and their download assets. For a Windows binary ZIP, extract it and run `MoonLander.exe` from **Windows Terminal** or **Command Prompt**. A compatible .NET Framework runtime is required.

This README describes version **2.0.0.0**; publication of its release package is a separate step. Check the release version before downloading.

### Option 2: Build from Source

#### Requirements

- **Windows** with **Visual Studio 2022 or a newer compatible version**, including the **.NET desktop development** workload
- **.NET Framework 4.7.2 targeting/developer pack**, or compatible reference assemblies for that target
- A compatible **.NET Framework runtime** to run the executable
- **PowerShell** to run the regression suite

#### To Build and Run

1. Clone the repository or download the source files.
2. Open `MoonLander/MoonLander.sln` or `MoonLander/MoonLander.csproj` in Visual Studio.
3. Select **Release** and **Rebuild Solution**. Close any running copy of the output executable first so it can be replaced.
4. Run `MoonLander/bin/Release/MoonLander.exe`.

Alternatively, from a Visual Studio Developer PowerShell at the repository root:

```powershell
msbuild .\MoonLander\MoonLander.sln /t:Rebuild /p:Configuration=Release
.\MoonLander\bin\Release\MoonLander.exe
```

### Controls and default configuration

Enter the fuel mass in kilograms for each step. Blank input skips the burn. Negative values, nonnumeric text, `NaN` and infinities are rejected and also skip the burn. Finite nonnegative requests are capped to:

```text
min(MaxBurnRateKgPerSec * TimeStep, remainingFuelMass)
```

The capped amount is spread over the entire configured step, even when it is less than the engine's maximum deliverable amount. Touchdown may shorten the actual burn. Numeric input and formatted output follow the current system culture.

The defaults in `MoonLander/Program.cs` are:

| Setting | Value |
| --- | --- |
| Initial altitude | 500 m |
| Initial velocity | −50 m/s |
| Initial spacecraft mass | 1000 kg |
| Dry mass / initial fuel | 800 kg / 200 kg |
| Relative exhaust speed | 2500 m/s |
| Gravity | −1.62 m/s² |
| Time step | 1 s |
| Maximum fuel flow | 20 kg/s |

## 🧪 Successful Landing Example

The `MoonLander/Test/SafeLanding.txt` fixture supplies eight zero burns, then 20 kg and 6 kg, followed by two zero burns. With the default configuration, it produces:

| Quantity | Result |
| --- | --- |
| Touchdown velocity | −2.78 m/s |
| Outcome | SAFE landing |
| Fuel remaining at touchdown | 174 kg |
| Total interaction energy delivered | 81.25 MJ (`8.1250000000E+007 J`) |
| Remaining fuel interaction energy | 543.75 MJ (`5.4375000000E+008 J`) |

Selected console output (energy and fuel lines are printed during stepping; the final velocity and total appear in the landing report):

```text
=== Vis Viva Moon Lander v2.0.0.0 ===
...
Fuel: 174.00 kg
...
Remaining Fuel Interaction Energy: 5.4375000000E+008 J
...
--- LANDING ---
Final velocity: -2.78 m/s
SAFE landing.
Theoretical Free-Fall Velocity: -64.19 m/s
...
Total Interaction Energy Delivered: 8.1250000000E+007 J
```

## Automated Regression Tests

Build the **Release** executable first. From PowerShell at the repository root, run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\MoonLander\Test\RunTests.ps1"
```

This starts a new PowerShell process. The execution-policy override applies only to that process and does not permanently change the system execution policy. The runner reads the eight `.txt` input fixtures in `MoonLander/Test/` and launches `MoonLander/bin/Release/MoonLander.exe`. It does not build the project, so rebuild after source changes. All fixture paths below are relative to the repository root.

| Fixture | Coverage |
| --- | --- |
| `MoonLander/Test/SafeLanding.txt` | Safe touchdown, fuel and corrected continuous-exhaust energy values |
| `MoonLander/Test/HiddenImpact.txt` | First contact despite a trajectory that would rise again within the step |
| `MoonLander/Test/FreeFall.txt` | Unpowered impact velocity and zero delivered interaction energy |
| `MoonLander/Test/PartialStepImpact.txt` | Partial-step fuel consumption and touchdown velocity |
| `MoonLander/Test/ContinuousBurn.txt` | Continuous-burn altitude, velocity and eventual impact |
| `MoonLander/Test/NegativeBurn.txt` | Rejection of negative fuel requests |
| `MoonLander/Test/InputValidation.txt` | Five rejected inputs and engine-flow capping |
| `MoonLander/Test/FuelDepletion.txt` | Ten full burns, empty fuel tank and subsequent zero-fuel cap |

The verified v2.0 suite passes **8/8 tests**. The runner checks selected formatted output strings and counts; it is a regression check, not a proof of correctness for every configuration. Expected strings use decimal points, so use a matching system culture when reproducing these checks.

## 📝 Model Limits

- Motion is vertical only, over a flat surface with constant gravity. There is no orbital dynamics, changing gravity, atmosphere, terrain or attitude control.
- Exhaust speed and fuel flow are idealized; there is no engine startup delay, throttle transient or efficiency model.
- Analytical stepping avoids ordinary time-integration truncation error for the stated within-step model. Floating-point evaluation and numerical contact root finding still have finite precision, and control choices remain discrete in time.
- The default configuration is the supported example. Configuration values are set in source; there is no comprehensive validation of arbitrary masses, gravity, time steps or engine parameters. Positive initial velocity is rejected by `Program.cs`.
- The theoretical free-fall velocity is a constant-gravity reference for the configured initial downward descent, not a prediction of a powered landing.
- The Vis Viva motivation is conceptual. Energy diagnostics do not add relativistic physics or constitute a complete engine, exhaust or impact-energy accounting model.

## 💬 A Continuing Conversation

From an HP-34C pocket-calculator landing exercise to a C# simulation, the project keeps its interest in what interactions can teach us: fuel ejection changes the descent, and contact ends it. Vis Viva provides an interaction-centred lens for exploring those events. That motivation is philosophical; the implemented equations and their stated Newtonian assumptions are the basis for the numerical results.

## 📁 License

This project is licensed under the GNU General Public License v3.0 (GPL-3.0). See [LICENSE](LICENSE) for the full licence text.
