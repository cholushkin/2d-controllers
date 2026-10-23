namespace Game.Gameplay.Controllers.Capsule
{
    // Cycles the fire button's meaning between Drill and Gun. Mouse wheel / RB / LB
    // all just call Next()/Previous() - with two entries they're equivalent, but kept
    // separate so a third weapon can slot in later without changing callers.
    public sealed class WeaponSelector
    {
        private static readonly WeaponId[] Order = { WeaponId.Drill, WeaponId.Gun };

        private int _index;

        public WeaponId Current => Order[_index];

        public void Next() => _index = (_index + 1) % Order.Length;
        public void Previous() => _index = (_index - 1 + Order.Length) % Order.Length;
    }
}
