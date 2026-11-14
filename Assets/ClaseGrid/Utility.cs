using UnityEngine;
//using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

/// <summary>Helpers from the class for integers, logs and sequences.</summary>
public static class Utility {
    /// <summary>Returns v limited to the range from min to max.</summary>
    public static int Clampi(int v, int min, int max)
    {
        return v < min ? min : (v > max ? max : v);
    }

	/// <summary>Logs message followed by param and returns param.</summary>
	public static T Log<T>(T param, string message = "") {
		Debug.Log(message +  param.ToString());
		return param;
	}

	/// <summary>Returns the infinite sequence seed, generator(seed), generator(generator(seed)) and so on.</summary>
	public static IEnumerable<Src> Generate<Src>(Src seed, Func<Src, Src> generator) {
		while (true) {
			yield return seed;
			seed = generator(seed);
		}
	}
}
