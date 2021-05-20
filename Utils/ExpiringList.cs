using System.Collections.Generic;
using System.Timers;

namespace MetalServer.Modules.UniChat.Utils {

	public class ExpiringList<T> : List<T> {

		public void Add(T newItem, double interval) {

			Add(newItem);

			using(Timer timer = new Timer(interval) {

				AutoReset = false,
				Enabled = true

			}) {

				timer.Elapsed += (sender, e) => {

					Remove(newItem);

				};

			}

		}

	}

}