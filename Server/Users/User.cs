using MetalServer.Modules.UniChat.Server.Message;
using System;
using static MetalServer.Modules.UniChat.Server.Message.Message.Query.Chat;

namespace MetalServer.Modules.UniChat.Server.Users {

	public class User {

		public const byte USER_KEY_LENGTH = 16;

		public UserTypes Type { get; }

		public string Name { get; set; }

		public string Key { get; }

		public string IP { get; }

		public Action<string, dynamic> updateListener;

		public User(UserTypes type, string name, string key, string ip, Action<string, dynamic> updateListener) {

			Type = type;
			Name = name;
			Key = key;
			IP = ip;
			this.updateListener = updateListener;

		}

		public void NewMessage(ChatMessage message, string key) {

			updateListener?.Invoke(Actions.ADD_CHAT_MESSAGE, new { message, key });

		}

		public void NewFile(ChatFile file, string key) {

			updateListener?.Invoke(Actions.ADD_CHAT_FILE, new { file, key });

		}

		public enum UserTypes {

			ANONYMOUS,
			PASSWORDLESS

		}

	}

}