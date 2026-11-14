namespace IA2
{
    /// <summary>
    /// Finite state machine driven by inputs.
    /// SendInput moves to the TargetState of the transition of the current state for that input.
    /// </summary>
    /// <typeparam name="T">Type of the inputs.</typeparam>
    public class EventFSM<T>
    {
        private State<T> current;

        /// <summary>The current state.</summary>
        public State<T> Current { get { return current; } }

        /// <summary>The input that led to the current state.</summary>
        public T CurrentKey { get; private set; }

        /// <summary>Creates the machine and enters initial.</summary>
        public EventFSM(State<T> initial)
        {
            current = initial;
            current.Enter(default(T));
        }

        /// <summary>
        /// Moves to the next state when the current state has a transition for input.
        /// Calls Exit on the current state and Enter on the next one.
        /// </summary>
        public void SendInput(T input)
        {
            State<T> newState;

            if (current.CheckInput(input, out newState))
            {
                current.Exit(input);
                current = newState;
                CurrentKey = input;
                current.Enter(input);
            }
        }

        /// <summary>Calls Update on the current state.</summary>
        public void Update()
        {
            current.Update();
        }

        /// <summary>Calls LateUpdate on the current state.</summary>
        public void LateUpdate()
        {
            current.LateUpdate();
        }

        /// <summary>Calls FixedUpdate on the current state.</summary>
        public void FixedUpdate()
        {
            current.FixedUpdate();
        }
    }
}
