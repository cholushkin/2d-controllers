using System;
using System.Collections.Generic;

namespace Game.Gameplay.Controllers.Core
{
    public sealed class ControllerStateMachine<TId> where TId : struct, Enum
    {
        private readonly Dictionary<TId, IControllerState<TId>> _states = new();
        private readonly ControllerContext _context;

        private IControllerState<TId> _current;
        private TId? _pendingTransition;

        public TId CurrentStateId => _current.Id;

        public ControllerStateMachine(ControllerContext context, IEnumerable<IControllerState<TId>> states, TId initial)
        {
            _context = context;

            foreach (var state in states)
                _states[state.Id] = state;

            _current = _states[initial];
            _current.Enter(_context);
        }

        // Transitions are requested here but only applied at the start of the next
        // Tick/FixedTick, so a state's own Tick/FixedTick never gets exited mid-call.
        public void RequestTransition(TId next)
        {
            if (!_states.ContainsKey(next))
                throw new KeyNotFoundException($"State '{next}' is not registered.");

            if (_current.Id.Equals(next))
                return;

            if (!_current.CanTransitionTo(next, _context))
                return;

            _pendingTransition = next;
        }

        public void Tick(float deltaTime)
        {
            ApplyPendingTransition();
            _current.Tick(_context, deltaTime);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            ApplyPendingTransition();
            _current.FixedTick(_context, fixedDeltaTime);
        }

        private void ApplyPendingTransition()
        {
            if (_pendingTransition is not { } next)
                return;

            _pendingTransition = null;
            _current.Exit(_context);
            _current = _states[next];
            _current.Enter(_context);
        }
    }
}
