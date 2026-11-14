using System;

namespace IA2
{
    /// <summary>
    /// Transition of a State to TargetState for Input.
    /// Raises OnTransition each time the transition executes.
    /// </summary>
    /// <typeparam name="T">Type of the inputs.</typeparam>
    public class Transition<T>
    {
        private T input;
        private State<T> targetState;

        /// <summary>Raised by OnTransitionExecute.</summary>
        public event Action<T> OnTransition = delegate { };

        /// <summary>Input of the transition.</summary>
        public T Input { get { return input; } }

        /// <summary>State the transition leads to.</summary>
        public State<T> TargetState { get { return targetState; } }

        /// <summary>Raises OnTransition.</summary>
        public void OnTransitionExecute(T input)
        {
            OnTransition(input);
        }

        /// <summary>Creates a transition to targetState for input.</summary>
        public Transition(T input, State<T> targetState)
        {
            this.input = input;
            this.targetState = targetState;
        }
    }
}
