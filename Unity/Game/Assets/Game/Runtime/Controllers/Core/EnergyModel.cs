using System;
using UnityEngine;

namespace Game.Gameplay.Controllers.Core
{
    // Plain class, no reactive bindings - UI/telemetry polls the properties or
    // subscribes to the plain Changed event.
    public sealed class EnergyModel
    {
        public event Action Changed;

        public int MaxBatteries { get; }
        public float MaxEnergy { get; }

        public int Batteries { get; private set; }
        public float Energy { get; private set; }

        public EnergyModel(int maxBatteries, float maxEnergy)
        {
            MaxBatteries = maxBatteries;
            MaxEnergy = maxEnergy;
            Batteries = maxBatteries;
            Energy = maxEnergy;
        }

        public bool TrySpendBattery()
        {
            if (Batteries <= 0)
                return false;

            Batteries--;
            Changed?.Invoke();
            return true;
        }

        public void RechargeBatteries(int amount)
        {
            Batteries = Mathf.Clamp(Batteries + amount, 0, MaxBatteries);
            Changed?.Invoke();
        }

        public bool TryDrainEnergy(float amount)
        {
            if (Energy <= 0f)
                return false;

            Energy = Mathf.Max(0f, Energy - amount);
            Changed?.Invoke();
            return true;
        }

        public void RechargeEnergy(float amount)
        {
            Energy = Mathf.Clamp(Energy + amount, 0f, MaxEnergy);
            Changed?.Invoke();
        }

        public void ResetFull()
        {
            Batteries = MaxBatteries;
            Energy = MaxEnergy;
            Changed?.Invoke();
        }
    }
}
