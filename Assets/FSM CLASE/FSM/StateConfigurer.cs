using System.Collections.Generic;

namespace IA2
{
    /// <summary>
    /// Builder for the transitions of a State.
    /// Done applies the transitions to the state.
    /// </summary>
    /// <typeparam name="T">Type of the inputs.</typeparam>
    public class StateConfigurer<T>
    {
        private State<T> instance;
        private Dictionary<T, Transition<T>> transitions = new Dictionary<T, Transition<T>>();

        /// <summary>Creates a configurer for state.</summary>
        public StateConfigurer(State<T> state) => instance = state;

        /// <summary>Adds the current transitions of the state to the configurer.</summary>
        public StateConfigurer<T> AddOldTransitions()
        {
            foreach (var item in instance.GetAllTransitions())
                transitions.Add(item.Key, item.Value);

            return this;
        }

        /// <summary>Removes the transition for key.</summary>
        public StateConfigurer<T> RemoveTransition(T key)
        {
            transitions.Remove(key);
            return this;
        }

        /// <summary>Adds a transition from the state to target for input.</summary>
        public StateConfigurer<T> SetTransition(T input, State<T> target)
        {
            transitions.Add(input, new Transition<T>(input, target));
            return this;
        }

        /// <summary>Applies the transitions to the state.</summary>
        public void Done() => instance.Configure(transitions);
    }

    /// <summary>Entry point for StateConfigurer with type inference.</summary>
    public static class StateConfigurer
    {
        /// <summary>Returns a new StateConfigurer for state.</summary>
        public static StateConfigurer<T> Create<T>(State<T> state)
        {
            return new StateConfigurer<T>(state);
        }
    }
}
