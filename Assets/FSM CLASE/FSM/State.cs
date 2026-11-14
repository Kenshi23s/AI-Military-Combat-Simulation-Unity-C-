using System;
using System.Collections.Generic;

namespace IA2
{
    /// <summary>
    /// State of an EventFSM with its transitions and its OnEnter, OnUpdate, OnLateUpdate, OnFixedUpdate and OnExit events.
    /// </summary>
    /// <typeparam name="T">Type of the inputs that trigger the transitions.</typeparam>
    public class State<T>
    {
        private string _stateName;
        private Dictionary<T, Transition<T>> transitions;

        /// <summary>Name of the state.</summary>
        public string Name { get { return _stateName; } }

        /// <summary>Raised by Enter with the input that led to this state.</summary>
        public event Action<T> OnEnter = delegate {};

        /// <summary>Raised by Update.</summary>
        public event Action OnUpdate = delegate {};

        /// <summary>Raised by LateUpdate.</summary>
        public event Action OnLateUpdate = delegate { };

        /// <summary>Raised by FixedUpdate.</summary>
        public event Action OnFixedUpdate = delegate { };

        /// <summary>Raised by Exit with the input that leads out of this state.</summary>
        public event Action<T> OnExit = delegate {};

        /// <summary>Creates a state with the given name.</summary>
        public State(string name)
        {
            _stateName = name;
        }

        /// <summary>Sets the transitions of the state and returns the state.</summary>
        public State<T> Configure(Dictionary<T, Transition<T>> transitions)
        {
            this.transitions = transitions;
            return this;
        }

        /// <summary>Returns the transition for input.</summary>
        public Transition<T> GetTransition(T input)
        {
            return transitions[input];
        }

        /// <summary>Returns a copy of the transitions of the state.</summary>
        public Dictionary<T, Transition<T>> GetAllTransitions()
        {
            return new Dictionary<T, Transition<T>>(transitions);
        }

        /// <summary>
        /// Returns true when the state has a transition for input.
        /// Executes the transition and sets next to its TargetState.
        /// </summary>
        /// <param name="input">Input to check.</param>
        /// <param name="next">TargetState of the transition. Set to this state when the method returns false.</param>
        public bool CheckInput(T input, out State<T> next)
        {
            if (transitions.TryGetValue(input, out var transition))
            {
                transition.OnTransitionExecute(input);
                next = transition.TargetState;
                return true;
            }

            next = this;
            return false;
        }

        /// <summary>Raises OnEnter.</summary>
        public void Enter(T input)
        {
            OnEnter(input);
        }

        /// <summary>Raises OnUpdate.</summary>
        public void Update()
        {
            OnUpdate();
        }

        /// <summary>Raises OnLateUpdate.</summary>
        public void LateUpdate()
        {
            OnLateUpdate();
        }

        /// <summary>Raises OnFixedUpdate.</summary>
        public void FixedUpdate()
        {
            OnFixedUpdate();
        }

        /// <summary>Raises OnExit.</summary>
        public void Exit(T input)
        {
            OnExit(input);
        }
    }
}
