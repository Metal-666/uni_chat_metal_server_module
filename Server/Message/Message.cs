using LiteDB;
using System.Collections.Generic;
using static MetalServer.Modules.UniChat.Server.Message.Message.Query.Chat;

namespace MetalServer.Modules.UniChat.Server.Message {

	public class Message {

		public Core core;

		public class Core {

			public string action, key;

		}

		public Auth auth;

		public class Auth {

			public string username, password;

		}

		public Query query;

		public class Query {

			public List<Chat> chatList;

			public Chat chat;

			public ChatMessage chatMesage;

			public ChatFile file;

			public class Chat {

				public ObjectId Id { get; set; }

				public string key { get; set; }
				public string name { get; set; }
				public string description { get; set; }
				public string password { get; set; }

				public List<ChatMessage> messages { get; set; }

				public List<ChatFile> files { get; set; }

				public List<string> usersWithAccess { get; set; }

				public Banner banner { get; set; }

				public Chat(string key) {

					this.key = key;

					messages = new List<ChatMessage>();
					files = new List<ChatFile>();
					usersWithAccess = new List<string>();

				}

				public class ChatFile {

					public string key { get; set; }
					public string chatKey { get; set; }
					public string extension { get; set; }
					public string name { get; set; }
					public string data { get; set; }

					[BsonIgnore]
					public string destination { get; set; }

					public static class Destinations {

						public const string DOWNLOAD = "DOWNLOAD",
											CHAT_STORAGE = "CHAT_STORAGE", ENLARGED_MESSAGE = "ENLARGED_MESSAGE";

					}

				}

				public class ChatMessage {

					public string key { get; set; }
					public string text { get; set; }
					public string type { get; set; }
					public string creator { get; set; }
					public long timestamp { get; set; }

					public List<ChatFile> files { get; set; }

					public ChatMessage(string key) {

						this.key = key;

					}

					public static class ChatMessageTypes {

						public const string BASIC = "BASIC",
											META = "META";

					}

				}

				public class Banner {

					public string imageId { get; set; }
					public string background { get; set; }

				}

			}

		}

		public Error error;

		public class Error {

			public string type, message;

			public class ErrorTypes {

				public const string CLIENT = "CLIENT",
									SERVER = "SERVER";

			}

		}

	}

}