using System.Collections.Generic;

namespace IA2 {
	public class StateConfigurer<T>
    {
		State<T> instance;
		Dictionary<T, Transition<T>> transitions = new Dictionary<T, Transition<T>>();

		public StateConfigurer(State<T> state) => instance = state;

        public StateConfigurer<T> AddOldTransitions()
		{
			foreach (var item in instance.GetAllTransitions())		
				transitions.Add(item.Key,item.Value);
      
			return this;
		}

        public  StateConfigurer<T> RemoveTransition(T key)
		{
			if (transitions.ContainsKey(key))			
				transitions.Remove(key);
			
			return this;
		}

        public StateConfigurer<T> SetTransition(T input, State<T> target) 
		{
			transitions.Add(input, new Transition<T>(input, target));
			return this;
		}

		public void Done() => instance.Configure(transitions);

    }

	public static class StateConfigurer
    {
		public static StateConfigurer<T> Create<T>(State<T> state)
        {
			return new StateConfigurer<T>(state);
		}

    }
}