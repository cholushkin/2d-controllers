namespace Game.Gameplay.Controllers.Core
{
    // Plain snapshot struct, polled on demand - not a reactive stream.
    public readonly struct ControllerTelemetry
    {
        public readonly string StateName;
        public readonly int Batteries;
        public readonly float Energy;

        public ControllerTelemetry(string stateName, int batteries, float energy)
        {
            StateName = stateName;
            Batteries = batteries;
            Energy = energy;
        }
    }
}
