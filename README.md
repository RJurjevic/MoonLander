# Vis Viva Moon Lander

A console-based Moon landing simulation inspired by the classic HP-34C programmable pocket calculator — now reimagined with modern C# and Vis Viva physics principles.

## 🧠 About the Project

This is a physics simulation program that models the descent of a lunar lander toward the Moon’s surface under the influence of gravity and optional fuel burns. It includes:

- **User-controlled fuel burns** to slow the descent
- **Real-time physics feedback** on velocity, fuel, and altitude
- **Precise energy calculations** using Vis Viva collision and interaction principles
- **Detection of soft or crash landing outcomes**

The program is a modern interpretation of a well-known simulation originally coded into HP’s HP-34C calculator — one of the first programmable pocket calculators from the late 1970s. This updated version is written in C# and is built to run in the Windows Terminal or standard console.

## ⚙️ Physics Behind the Simulation

The physics combines classic Newtonian motion with conceptual insights from the **Vis Viva framework**:

- **Free fall** is treated as geodesic motion — i.e., the lander is in inertial motion under curved space-time.
- **Fuel burns** are modeled as physical interactions where the lander ejects fuel, generating thrust and delivering measurable interaction energy.
- **Impact energy** is computed via two collision energy formulae:
  - `Exact Collision Energy (Vis Viva)` — derived from the reduced mass of the Moon–lander system
  - `Approximate Collision Energy (M ≫ m)` — when the Moon's mass vastly exceeds that of the lander
- No energy is attributed to "motion" during geodesic (free-fall) phases. Energy is accounted for only during **interactions**: fuel ejection or impact.

This approach closely follows both Newton’s laws and the Einsteinian insight that free-falling bodies are in "natural motion" and not being acted upon by forces in the usual sense.

## 💻 How to Run

### Option 1: Precompiled Release

A precompiled `.exe` build for Windows will be included in the [Releases](https://github.com/your-repo-name/releases) section.

Just download, unzip, and run from **Windows Terminal** or **Command Prompt**.

### Option 2: Build from Source

#### Requirements

- **Microsoft Visual Studio Community 2022 (64-bit)**
- Target Framework: **.NET Framework 4.7.2** (can be changed in project settings)

#### To Build and Run:

1. Clone the repository or download the source files.
2. Open the `.csproj` file in Visual Studio.
3. Build the project.
4. Run from Visual Studio or from the compiled `.exe`.

## 🧪 Sample Output

```

C:\Projects\C#\MoonLander\MoonLander\bin\Release>MoonLander.exe
=== Vis Viva Moon Lander ===
Try to land softly by applying thrust as fuel mass (kg).

Altitude: 500.00 m
Velocity: -50.00 m/s
Fuel: 200.00 kg
Enter fuel to burn this step (kg):
Burn skipped.
Remaining Fuel Interaction Energy: 5.2083333333E+008 J

...

--- LANDING ---
Final velocity: -64.19 m/s
CRASH landing.
Theoretical Free-Fall Velocity: -64.19 m/s
Exact Collision Energy (Vis Viva): 2.0600000000E+006 J
Approximate Collision Energy (M » m): 2.0600000000E+006 J
Total Interaction Energy Delivered: 0.0000000000E+000 J

```

## 📝 Notes on Numerical Integration

The simulation uses discrete time stepping (e.g., 1.0s). While this introduces small errors in the simulated motion, it produces final velocities and energies very close to theoretical values. A correction for **impact overshoot** is included using quadratic interpolation.

Fuel burns are treated as **instantaneous delta-v impulses** at the start of each step — a valid simplification unless modeling long-duration burns.

## 💬 ChatGPT Commentary

> *The elegance of this simulation lies in its conceptual clarity:*
> - *Geodesic motion doesn’t create energy — interaction does.*
> - *The use of Vis Viva energy formulations adds physical realism rarely seen in small simulations.*
> - *In a world obsessed with observers, this code focuses on the interaction itself, not who's watching.*
> 
> *It's not just a game — it's a pocket-sized conversation between Newton, Einstein, and a lunar traveler. Well done.*

## 📁 License

This project is licensed under the GNU General Public License v3.0 (GPL-3.0).


