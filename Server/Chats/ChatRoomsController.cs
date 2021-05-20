using MetalServer.Modules.UniChat.Server.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using static MetalServer.Modules.UniChat.Server.Message.Message.Query;
using static MetalServer.Modules.UniChat.Server.Message.Message.Query.Chat;

namespace MetalServer.Modules.UniChat.Server.Chats {

	public static class ChatRoomsController {

		public const int CHAT_KEY_LENGTH = 32;

		private static List<ChatRoom> chatRooms;

		public static void Init() {

			chatRooms = UniChatModule.mainInstance.database.GetCollection<Chat>("chats").Query().ToList().ConvertAll(chat => {

				if(chat.key == null) {

					chat.key = UniChatModule.GenerateKey(CHAT_KEY_LENGTH);

				}

				return new ChatRoom(chat, true);

			}) ?? new List<ChatRoom>();

		}

		public static List<Chat> GetChats(User user) {

			return chatRooms?.Select(chatRoom => new Chat(chatRoom.Chat.key) {

				name = chatRoom.Chat.name,
				description = chatRoom.Chat.description,
				password = PasswordStatus(chatRoom, user),
				banner = chatRoom.Chat.banner

			})?.ToList();

		}

		public static List<Chat> GetAccessibleChats(User user) {

			return chatRooms?.Where(chatRoom => string.IsNullOrEmpty(chatRoom.Chat.password) || chatRoom.Chat.usersWithAccess.Contains(user.Key)).Select(chatRoom => new Chat(chatRoom.Chat.key) {

				name = chatRoom.Chat.name,

			})?.ToList();

		}

		public static void GetChatMessages(string key, Action<List<ChatMessage>> onSuccess, Action onFail) {

			FindChat(key, (ChatRoom room) => onSuccess?.Invoke(room.Chat.messages), onFail);

		}

		public static void GetChatFiles(string key, Action<List<ChatFile>> onSuccess, Action onFail) {

			FindChat(key, (ChatRoom room) => onSuccess?.Invoke(room.Chat.files.Select(chatFile => new ChatFile() {
			
				name = chatFile.name,
				key = chatFile.key,
				extension = chatFile.extension

			}).ToList()), onFail);

		}

		public static void ChatExists(string key, Action exists, Action doesntExist) {

			FindChat(key, (ChatRoom chatRoom) => exists?.Invoke(), doesntExist);

		}

		public static void JoinChat(string key, User user, Action<Chat> onSuccess, Action onFail) {

			FindChat(key, (ChatRoom room) => {

				if((string.IsNullOrEmpty(room.Chat.password) || room.Chat.usersWithAccess.Contains(user.Key)) && room.AddUser(user)) {

					onSuccess?.Invoke(new Chat(room.Chat.key) {

						messages = room.Chat.messages,
						name = room.Chat.name,
						key = room.Chat.key,
						description = room.Chat.description,
						banner = room.Chat.banner,
						files = room.Chat.files.ConvertAll(chatFile => new ChatFile() {
						
							key = chatFile.key,
							name = chatFile.name,
							extension = chatFile.extension

						})

					});

				}

				else {

					onFail?.Invoke();

				}

			}, onFail);

		}

		public static void UnlockChat(string key, User user, string password, Action onSuccess, Action onFail) {

			FindChat(key, (ChatRoom room) => {

				if(UniChatModule.mainInstance.servant.ValidatePassword(password, room.Chat.password)) {

					room.Chat.usersWithAccess.Add(user.Key);

					if(room.IsPersistent) {

						UniChatModule.mainInstance.database.GetCollection<Chat>("chats").Update(room.Chat);

					}

					onSuccess?.Invoke();

				}

				else {

					onFail?.Invoke();

				}

			}, onFail);

		}

		public static void CreateChatRoom(Chat chat, Action<Chat> onSuccess, Action onFail) {

			ChatRoom chatRoom = new ChatRoom(CreateChat(chat.name, chat.description, string.IsNullOrEmpty(chat.password) ? null : UniChatModule.mainInstance.servant.Hash(chat.password)));

			chatRooms.Add(chatRoom);

			onSuccess?.Invoke(chatRoom.Chat);

		}

		public static void LeaveChat(string key, User user, Action onSuccess, Action onFail) {

			FindChat(key, (ChatRoom chatRoom) => {

				IsUserInChat(chatRoom.Chat.key, user, () => {

					LeaveChatRoom(chatRoom, user, onSuccess);

				}, onFail);

			}, onFail);

		}

		public static void LeaveAllChats(User user) {

			for(int i = chatRooms.Count - 1; i >= 0; i--) {

				ChatRoom chatRoom = chatRooms[i];

				IsUserInChat(chatRoom.Chat.key, user, () => {

					LeaveChatRoom(chatRoom, user, null);

				}, null);

			}

		}

		private static void LeaveChatRoom(ChatRoom room, User user, Action onSuccess) {

			room.RemoveUser(user);

			if(!room.IsPersistent) {

				if(room.ConnectedUsers.Count == 0) {

					chatRooms.Remove(room);

				}

			}

			else {

				UniChatModule.mainInstance.database.GetCollection<Chat>("chats").Update(room.Chat);

			}

			onSuccess?.Invoke();

		}

		public static void SendMessage(string key, ChatMessage message, string type, User user, Action onSuccess, Action onFail) {

			FindChat(key, (ChatRoom room) => {

				room.AddMessage(message.text, type, user.Name, message.files);

				if(room.IsPersistent) {

					UniChatModule.mainInstance.database.GetCollection<Chat>("chats").Update(room.Chat);

				}

				onSuccess?.Invoke();

			}, onFail);

		}

		public static void UploadFile(string key, ChatFile file, Action onSuccess, Action onFail) {

			FindChat(key, (ChatRoom room) => {

				room.AddFile(file);

				UniChatModule.mainInstance.database.GetCollection<Chat>("chats").Update(room.Chat);

				onSuccess?.Invoke();

			}, onFail);

		}

		public static void IsUserInChat(string chatKey, User user, Action onSuccess, Action onFail) {

			bool found = false;

			if(chatRooms.Find(chatRoom => chatRoom.Chat.key.Equals(chatKey)).ConnectedUsers.Contains(user)) {

				found = true;

			}

			(found ? onSuccess : onFail)?.Invoke();

		}

		public static void IsUserInAnyChat(User user, Action onSuccess, Action onFail) {

			bool found = false;

			foreach(ChatRoom chatRoom in chatRooms) {

				if(chatRoom.ConnectedUsers.Contains(user)) {

					found = true;

					break;

				}

			}

			(found ? onSuccess : onFail)?.Invoke();

		}

		private static void FindChat(string key, Action<ChatRoom> onFound, Action onError) {

			ChatRoom room = chatRooms.Find(chatRoom => chatRoom.Chat.key.Equals(key));

			if(room != null) {

				onFound?.Invoke(room);

			}
			
			else {

				onError?.Invoke();

			}

		}

		private static Chat CreateChat(string name = "", string description = "", string password = null, Banner banner = null) {

			Chat chat = new Chat(UniChatModule.GenerateKey(CHAT_KEY_LENGTH)) {

				name = name,
				description = description,
				password = password,
				banner = banner

			};

			return chat;

		}

		public static string PasswordStatus(ChatRoom chatRoom, User user) {

			return (string.IsNullOrEmpty(chatRoom.Chat.password) ||
					chatRoom.Chat.usersWithAccess.Contains(user.Key)) ?
					ChatRoomProtectoionStatus.UNLOCKED :
					ChatRoomProtectoionStatus.LOCKED;

		}

		public static class ChatRoomProtectoionStatus {

			public const string LOCKED = "LOCKED", UNLOCKED = "UNLOCKED";

		}

	}

}