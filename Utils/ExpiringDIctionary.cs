using System.Collections.Generic;
using System.Timers;

namespace MetalServer.Modules.UniChat.Utils {

	public class ExpiringDictionary<T1, T2> : Dictionary<T1, T2> {

		public void Add(T1 key, T2 value, double interval) {

			Add(key, value);

			using(Timer timer = new Timer(interval) {

				AutoReset = false,
				Enabled = true

			}) {

				timer.Elapsed += (sender, e) => {

					Remove(key);

				};

			}

		}

	}

}